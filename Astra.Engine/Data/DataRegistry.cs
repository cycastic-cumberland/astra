using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Collections;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.Engine.Aggregator;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;
using Astra.Engine.Types;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace Astra.Engine.Data;

using SynthesizersRead = (IIndexer.IIndexerReadHandler? handler, IColumnResolver resolver);
using SynthesizersWrite = (IIndexer.IIndexerWriteHandler? handler, IColumnResolver resolver);

public class WriteOperationsNotAllowed(string? msg = null) : Exception(msg);

public class DuplicatedColumnNameException(string? msg = null) : Exception(msg);

public sealed class DataRegistry : IRegistry<DataRegistry>
{
    public interface IIndexersLock : IDisposable
    {
        public int Count { get; }
        public void Read(int index, Action<SynthesizersRead> action);
        public void Read<TIn>(int index, TIn payload, Action<SynthesizersRead, TIn> action);
        public T Read<T>(int index, Func<SynthesizersRead, T> action);
        public T Read<T, TIn>(int index, TIn payload, Func<SynthesizersRead, TIn, T> action);
    }
    public struct IndexersWriteLock(SynthesizersWrite[] synthesizers, ILogger<IndexersWriteLock> logger) 
        : ITransaction, IIndexersLock, IReadOnlyCollection<SynthesizersWrite>
    {
        private bool _finalized = false;
        public void Dispose()
        {
            if (_finalized) return;
            _finalized = true;
            foreach (var synthesizer in synthesizers)
            {
                try
                {
                    synthesizer.handler?.Dispose();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception occured while releasing writer lock");
                }
            }
            logger.LogDebug("Indexers' state released");
        }

        public void Commit()
        {
            if (_finalized) return;
            _finalized = true;
            foreach (var synthesizer in synthesizers)
            {
                try
                {
                    synthesizer.handler?.Commit();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception occured while commiting writer lock");
                }
            }
            logger.LogDebug("Indexers' state committed");
        }

        public void Rollback()
        {
            if (_finalized) return;
            _finalized = true;
            foreach (var synthesizer in synthesizers)
            {
                try
                {
                    synthesizer.handler?.Rollback();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception occured while rolling back writer lock");
                }
            }
            logger.LogDebug("Indexers' state rolled back");
        }

        public int Count { get; } = synthesizers.Length;

        public void Read(int index, Action<SynthesizersRead> action)
            => action(synthesizers[index]);

        public void Read<TIn>(int index, TIn payload, Action<SynthesizersRead, TIn> action)
            => action(synthesizers[index], payload);
        
        public T Read<T>(int index, Func<SynthesizersRead, T> action)
            => action(synthesizers[index]);

        public T Read<T, TIn>(int index, TIn payload, Func<SynthesizersRead, TIn, T> action)
            => action(synthesizers[index], payload);

        public IEnumerator<SynthesizersWrite> GetEnumerator()
        {
            return ((IEnumerable<SynthesizersWrite>)synthesizers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly struct AutoReadLock(DataRegistry registry, ILogger<AutoReadLock> logger) : IIndexersLock
    {
        public int Count => registry._synthesizers.Length;
        public void Dispose()
        {
            
        }

        public void Read(int index, Action<SynthesizersRead> action)
        {
            var handler = registry._synthesizers[index].Read();
            try
            {
                action(handler);
            }
            finally
            {
                handler.handler?.Dispose();
            }
        }

        public void Read<TIn>(int index, TIn payload, Action<SynthesizersRead, TIn> action)
        {
            var handler = registry._synthesizers[index].Read();
            try
            {
                action(handler, payload);
            }
            finally
            {
                handler.handler?.Dispose();
            }
        }

        public T Read<T>(int index, Func<SynthesizersRead, T> action)
        {
            var handler = registry._synthesizers[index].Read();
            try
            {
                return action(handler);
            }
            finally
            {
                handler.handler?.Dispose();
            }
        }

        public T Read<T, TIn>(int index, TIn payload, Func<SynthesizersRead, TIn, T> action)
        {
            var handler = registry._synthesizers[index].Read();
            try
            {
                return action(handler, payload);
            }
            finally
            {
                handler.handler?.Dispose();
            }
        }
    }
    
    public struct StreamBasedAggregationEnumerator<T> : IEnumerator<T>
        where T : IAstraSerializable
    {
        private AutoReadLock _lock;
        private LocalAggregatorEnumerator<T, AutoReadLock> _enumerator;
        internal StreamBasedAggregationEnumerator(DataRegistry host, Stream stream)
        {
            _lock = new(host, host._readLogger);
            _enumerator = new (stream, _lock);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            _lock.Dispose();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current;
    }
    
    public readonly struct StreamBasedAggregationEnumerable<T> : IEnumerable<T>
        where T : IAstraSerializable
    {
        private readonly DataRegistry _host;
        private readonly Stream _stream;

        internal StreamBasedAggregationEnumerable(DataRegistry host, Stream stream)
        {
            _host = host;
            _stream = stream;
        }

        public StreamBasedAggregationEnumerator<T> GetEnumerator()
        {
            return new(_host, _stream);
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    public struct DynamicStreamBasedAggregationEnumerator<T> : IEnumerator<T>
    {
        private StreamBasedAggregationEnumerator<FlexSerializable<T>> _enumerator;
        internal DynamicStreamBasedAggregationEnumerator(DataRegistry host, Stream stream)
        {
            _enumerator = new(host, stream);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public T Current => _enumerator.Current.Target;

        object IEnumerator.Current => Current!;
    }
    
    public readonly struct DynamicStreamBasedAggregationEnumerable<T> : IEnumerable<T>
    {
        private readonly DataRegistry _host;
        private readonly Stream _stream;

        internal DynamicStreamBasedAggregationEnumerable(DataRegistry host, Stream stream)
        {
            _host = host;
            _stream = stream;
        }

        public DynamicStreamBasedAggregationEnumerator<T> GetEnumerator()
        {
            return new(_host, _stream);
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct BufferBasedAggregationEnumerator<T> : IEnumerator<T>
        where T : IAstraSerializable
    {
        private readonly ReadOnlyBufferStream _bufferStream;
        private StreamBasedAggregationEnumerator<T> _enumerator;
        internal BufferBasedAggregationEnumerator(DataRegistry host, ReadOnlyMemory<byte> predicate)
        {
            _bufferStream = LocalBuffer.Value ?? new();
            LocalBuffer.Value = null;
            _bufferStream.Buffer = predicate;
            _enumerator = new(host, _bufferStream);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            LocalBuffer.Value = _bufferStream;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current;
    }

    public readonly struct BufferBasedAggregationEnumerable<T> : IEnumerable<T>
        where T : IAstraSerializable
    {
        private readonly DataRegistry _host;
        private readonly ReadOnlyMemory<byte> _predicate;

        internal BufferBasedAggregationEnumerable(DataRegistry host, ReadOnlyMemory<byte> predicate)
        {
            _host = host;
            _predicate = predicate;
        }

        public BufferBasedAggregationEnumerator<T> GetEnumerator()
        {
            return new(_host, _predicate);
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct DynamicBufferBasedAggregationEnumerator<T> : IEnumerator<T>
    {
        private BufferBasedAggregationEnumerator<FlexSerializable<T>> _enumerator;

        internal DynamicBufferBasedAggregationEnumerator(DataRegistry host, ReadOnlyMemory<byte> predicate)
        {
            _enumerator = new(host, predicate);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public T Current => _enumerator.Current.Target;

        object IEnumerator.Current => Current!;
    }
    
    public readonly struct DynamicBufferBasedAggregationEnumerable<T> : IEnumerable<T, DynamicBufferBasedAggregationEnumerator<T>>
    {
        private readonly DataRegistry _host;
        private readonly ReadOnlyMemory<byte> _predicate;

        internal DynamicBufferBasedAggregationEnumerable(DataRegistry host, ReadOnlyMemory<byte> predicate)
        {
            _host = host;
            _predicate = predicate;
        }

        public DynamicBufferBasedAggregationEnumerator<T> GetEnumerator()
        {
            return new(_host, _predicate);
        }
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct Enumerator<T> : IEnumerator<T>
    {
        private AutoIndexer.ReadHandler _auto;
        private AutoReadLock _read;
        private PreparedLocalAggregatorEnumerator<FlexSerializable<T>, AutoReadLock> _enumerator;

        public Enumerator(DataRegistry host)
        {
            _auto = host._autoIndexer.Read();
            _read = new(host, host._readLogger);
            _enumerator = new(_read, _auto.FetchAllUnsafe());
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            _read.Dispose();
            _auto.Dispose();
        }

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public T Current => _enumerator.Current.Target;

        object IEnumerator.Current => Current!;
    }

    public readonly struct Enumerable<T>(DataRegistry host) : IEnumerable<T>
    {
        public Enumerator<T> GetEnumerator()
        {
            return new(host);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    private static readonly ThreadLocal<ReadOnlyBufferStream?> LocalBuffer = new();
    private static readonly ThreadLocal<RecyclableMemoryStream?> LocalBulkInsertStream = new();
    
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DataRegistry> _logger;
    private readonly ILogger<IndexersWriteLock> _writeLogger;
    private readonly ILogger<AutoReadLock> _readLogger;
    private readonly int _rowSize;
    private readonly int _hashSize;
    private readonly int _indexerCount;
    private readonly AbstractRegistryDump _dump;
    private readonly AutoIndexer _autoIndexer = new();
    private readonly ColumnSynthesizer[] _synthesizers;
    private readonly string[] _columnNames;
    private readonly ReadOnlyDictionary<Dictionary<string, ColumnSynthesizer>, string, ColumnSynthesizer> _nameToResolvers;

    public int ColumnCount => _synthesizers.Length;
    public int IndexedColumnCount => _synthesizers.Length;

    public static DataRegistry Fabricate(RegistrySchemaSpecifications schema, ILoggerFactory? loggerFactory = null,
        AbstractRegistryDump? dump = null)
    {
        return new(schema, loggerFactory: loggerFactory, dump: dump);
    }

    public int RowsCount
    {
        get
        {
            using var autoIndexerLock = _autoIndexer.Read();
            return autoIndexerLock.Count;
        }
    }
    public DataRegistry(RegistrySchemaSpecifications schema, ILoggerFactory? loggerFactory = null, IReadOnlyDictionary<uint, ITypeHandler>? handlers = null, AbstractRegistryDump? dump = null)
    {
        handlers ??= TypeHandler.Default;
        var nameToResolvers = new Dictionary<string, ColumnSynthesizer>();
        if (loggerFactory == null)
        {
            _loggerFactory = LoggerFactory.Create(_ =>
            {

            });
            loggerFactory = _loggerFactory;
        }
        _logger = loggerFactory.CreateLogger<DataRegistry>();
        _writeLogger = loggerFactory.CreateLogger<IndexersWriteLock>();
        _readLogger = loggerFactory.CreateLogger<AutoReadLock>();
        _dump = dump ?? AbstractRegistryDump.Empty;
        _synthesizers = new ColumnSynthesizer[schema.Columns.Length];
        _columnNames = new string[schema.Columns.Length];
        var i = 0;
        var offset = 0;
        var hashSize = 0;
        var indexerCount = 0;
        foreach (var column in schema.Columns)
        {
            var handler = handlers[column.DataType];
            var result = handler.Process(column, schema, i, offset);
            offset = result.NewOffset;
            var shouldBeHashed = result.IsHashed;
            var dataType = result.TypeName;
            var resolver = result.Resolver;
            var indexer = result.Indexer;

            if (shouldBeHashed)
            {
                hashSize += resolver.HashSize;
            }
            _logger.LogDebug("Column {}: found type: {}, indexer: {}, should be hashed: {}",
                i, dataType, column.Indexer, shouldBeHashed);
            if (indexer != null)
            {
                indexerCount++;
            }
            var synth = ColumnSynthesizer.Create(indexer, resolver);
            if (!nameToResolvers.TryAdd(column.Name, synth))
                throw new DuplicatedColumnNameException(column.Name);
            var index = i++;
            _synthesizers[index] = synth;
            _columnNames[index] = column.Name;
        }
        _rowSize = offset;
        _hashSize = hashSize;
        _indexerCount = indexerCount;
        _nameToResolvers = new(nameToResolvers);
        _logger.LogInformation("Row length: {} byte(s)", _rowSize);
        _logger.LogInformation("Hash stream length: {} byte(s)", _hashSize);
    }
    
    private IndexersWriteLock AcquireWriteLock()
    {
        var s = new SynthesizersWrite[_synthesizers.Length];
        var i = 0;
        foreach (var synthesizer in _synthesizers)
        {
            s[i++] = synthesizer.Write();
        }

        return new(s, _writeLogger);
    }
    
    public void Dispose()
    {
        using var autoIndexerLock = _autoIndexer.Write();
        if (!_dump.CanBeDumped) return;
        var stream = _dump.PrepareStream();
        try
        {
            SerializeInternal(stream, autoIndexerLock);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during dumping table");
        }
        finally
        {
            _dump.CloseStream(stream);
            _loggerFactory?.Dispose();
        }
    }

    private IEnumerable<ImmutableDataRow> FetchAll()
    {
        using var reader = _autoIndexer.Read();
        using var enumerator = reader.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }
    
    public StreamBasedAggregationEnumerable<T> AggregateCompat<T>(Stream predicateStream) where T : IAstraSerializable
    {
        return new(this, predicateStream);
    }
    
    public BufferBasedAggregationEnumerable<T> AggregateCompat<T>(ReadOnlyMemory<byte> predicateStream) where T : IAstraSerializable
    {
        return new(this, predicateStream);
    }
    
    public DynamicStreamBasedAggregationEnumerable<T> Aggregate<T>(Stream predicateStream)
    {
        return new(this, predicateStream);
    }

    IEnumerable<T> IRegistry.Aggregate<T>(Stream predicateStream)
    {
        return Aggregate<T>(predicateStream);
    }
    
    public DynamicBufferBasedAggregationEnumerable<T> Aggregate<T>(ReadOnlyMemory<byte> predicateStream)
    {
        return new(this, predicateStream);
    }

    IEnumerable<T> IRegistry.Aggregate<T>(ReadOnlyMemory<byte> predicate)
    {
        return Aggregate<T>(predicate);
    }

    public Enumerator<T> GetEnumerator<T>() => new(this);
    IEnumerator<T> IRegistry.GetEnumerator<T>() => GetEnumerator<T>();
    
    private static int ConditionalCountInternal<T>(Stream predicateStream, T indexersLock) where T : struct, DataRegistry.IIndexersLock
    {
        var set = predicateStream.Aggregate(indexersLock);
        return set?.Count() ?? 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ConditionalCount(Stream predicateStream)
    {
        using var readLock = new AutoReadLock(this, _readLogger);
        return ConditionalCountInternal(predicateStream, readLock);
    }

    private static void CountAll<T>(AutoIndexer.WriteHandler writeHandler, T outStream) where T : Stream
    {
        outStream.WriteValue(writeHandler.Count);
    }
    

    private int DeleteRows(Stream predicateStream, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var set = predicateStream.Aggregate(writeLock);
        var count = 0;
        if (set != null)
        {
            foreach (var row in set)
            {
                autoIndexerLock.RemoveExact(row);
                foreach (var write in writeLock)
                {
                    write.handler?.RemoveExact(row);
                }
                row.Dispose();
                count++;
            }
        }
        _logger.LogDebug("{} row(s) deleted", count);
        return count;
    }

    public int Delete(Stream predicateStream)
    {
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        return DeleteRows(predicateStream, autoIndexerLock, writeLock);
    }
    
    public int Delete(ReadOnlyMemory<byte> predicateStream)
    {
        var buffer = LocalBuffer.Value ?? new(); 
        buffer.Buffer = predicateStream;
        LocalBuffer.Value = buffer;
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        return DeleteRows(buffer, autoIndexerLock, writeLock);
    }

    private void SerializeInternal<T>(Stream writer, T autoIndexerLock)
        where T : struct, IIndexer.IIndexerReadHandler
    {
        using var enumerator = autoIndexerLock.GetEnumerator();
        // Apparently using foreach here would box autoIndexerLock
        while (enumerator.MoveNext())
        {
            var row = enumerator.Current;
            foreach (var synthesizer in _synthesizers)
            {
                synthesizer.Resolver.Serialize(writer, row);
            }
        }
    }
    
    public void Serialize(Stream writer)
    {
        using var autoIndexerLock = _autoIndexer.Read();
        SerializeInternal(writer, autoIndexerLock);
    }

    private bool InsertOne(Stream reader, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var immutableDataRow = DataRow.CreateImmutable(reader, _synthesizers, _rowSize, _hashSize, _synthesizers.Length);
    
        try
        {
            if (autoIndexerLock.Contains(immutableDataRow))
            {
                _logger.LogDebug("Row with hash '{}' existed", immutableDataRow.Hash);
                immutableDataRow.Dispose();
                return false;
            }
            autoIndexerLock.Add(immutableDataRow);
            foreach (var synthesizer in writeLock)
            {
                synthesizer.handler?.Add(immutableDataRow);
            }
            _logger.LogDebug("Row inserted: {}", immutableDataRow.Hash);
        }
        catch (Exception e)
        {
            var hash = immutableDataRow.Hash;
            _logger.LogError(e, "Exception caught while adding row '{}'", hash);
            immutableDataRow.Dispose();
            throw;
        }

        return true;
    }
    
    private int Insert(Stream reader, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var rowCount = reader.ReadInt();
        var inserted = 0;
        for (var i = 0; i < rowCount; i++)
        {
            _ = InsertOne(reader, autoIndexerLock, writeLock) ? ++inserted : 0;
        }
        return inserted;
    }

    private void WriteOnce<TIn, TOut>(TIn dataIn, TOut dataOut,  AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock) where TIn : Stream where TOut : Stream
    {
        var command = dataIn.ReadUInt();
        switch (command)
        {
#if DEBUG
            case Command.HelloWorld:
            {
                var str = dataIn.ReadString();
                var valOut = $"Hello, {str}";
                dataOut.WriteValue(valOut);
                _logger.LogInformation("{}", valOut);
                break;
            }
#endif
            case Command.UnsortedAggregate:
            {
                OutputLayout(dataOut, _synthesizers);
                dataIn.AggregateStream(dataOut, writeLock);
                break;
            }
            case Command.UnsortedInsert:
            {
                var inserted = Insert(dataIn, autoIndexerLock, writeLock);
                dataOut.WriteValue(inserted);
                break;
            }
            case Command.ConditionalDelete:
            {
                var deleted = DeleteRows(dataIn, autoIndexerLock, writeLock);
                dataOut.WriteValue(deleted);
                break;
            }
            case Command.CountAll:
            {
                CountAll(autoIndexerLock, dataOut);
                break;
            }
            case Command.ConditionalCount:
            {
                var count = ConditionalCountInternal(dataIn, writeLock);
                dataOut.WriteValue(count);
                break;
            }
            case Command.Clear:
            {
                var deleted = Clear(autoIndexerLock, writeLock);
                dataOut.WriteValue(deleted);
                break;
            }
            default:
                throw new CommandNotSupported($"Command code not found: {command}");
        }
    }
    
    private void ReadOnce<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream
    {
        var command = dataIn.ReadUInt();
        switch (command)
        {
            case Command.UnsortedAggregate:
            {
                using var readLock = new AutoReadLock(this, _readLogger);
                OutputLayout(dataOut, _synthesizers);
                dataIn.AggregateStream(dataOut, readLock);
                break;
            }
            case Command.CountAll:
            {
                dataOut.WriteValue(RowsCount);
                break;
            }
            case Command.ConditionalCount:
            {
                using var readLock = new AutoReadLock(this, _readLogger);
                var count = ConditionalCountInternal(dataIn, readLock);
                dataOut.WriteValue(count);
                break;
            }
            case Command.UnsortedInsert:
            case Command.ConditionalDelete:
            case Command.Clear:
                throw new WriteOperationsNotAllowed("This frame can only execute read commands");
            default:
                throw new CommandNotSupported($"Command code not found: {command}");
        }
    }
    
    // dataIn layout
    // [header[command[description]]]
    //  1      4       >= 0
    //
    // dataOut layout
    // [is_faulted][results]
    //  1           >= 0
    public void ConsumeStream<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream
    {
        dataOut.WriteByte(0);
        var commandHeader = dataIn.ReadUInt();
        var (commandCount, enableWrite) = Command.SplitCommandHeader(commandHeader);
        if (enableWrite)
        {
            using var autoIndexerLock = _autoIndexer.Write();
            using var writeLock = AcquireWriteLock();
            for (var i = 0U; i < commandCount; i++)
                WriteOnce(dataIn, dataOut, autoIndexerLock, writeLock);
            writeLock.Commit();
            autoIndexerLock.Commit();
            return;
        }

        for (var i = 0U; i < commandCount; i++)
            ReadOnce(dataIn, dataOut);
    }

    public bool InsertCompat<T>(T value) where T : IAstraSerializable
    {
        using var inStream = MemoryStreamPool.Allocate();
        value.SerializeStream(new ForwardStreamWrapper(inStream));
        inStream.Position = 0;
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        return InsertOne(inStream, autoIndexerLock, writeLock);
    }

    public bool Insert<T>(T value)
    {
        return InsertCompat(new FlexSerializable<T> { Target = value });
    }
    
    public int BulkInsertCompat<T>(IEnumerable<T> values) where T : IAstraSerializable
    {
        var inStream = LocalBulkInsertStream.Value ?? MemoryStreamPool.Allocate();
        var wrapper = new ForwardStreamWrapper(inStream);
        try
        {
            var count = 0;
            using var autoIndexerLock = _autoIndexer.Write();
            using var writeLock = AcquireWriteLock();
            foreach (var value in values)
            {
                inStream.SetLength(0);
                value.SerializeStream(wrapper);
                inStream.Position = 0;
                _ = InsertOne(inStream, autoIndexerLock, writeLock) ? count++ : 0;
            }

            return count;
        }
        finally
        {
            inStream.SetLength(0);
            LocalBulkInsertStream.Value = inStream;
        }
    }

    public int BulkInsert<T>(IEnumerable<T> values)
    {
        return BulkInsertCompat(values.Select(o => new FlexSerializable<T> { Target = o }));
    }

    private static int Clear(AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var count = autoIndexerLock.Clear();
        foreach (var (indexer, resolver) in writeLock)
        {
            if (indexer != null)
            {
                indexer.Clear();
                continue;
            }
            resolver.Clear();
        }
        return count;
    }
    
    public int Clear()
    {
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        var ret = Clear(autoIndexerLock, writeLock);
        autoIndexerLock.Commit();
        writeLock.Commit();
        return ret;
    }


    private static void OutputLayout<TStream, TList>(TStream stream, TList synthesizers) 
        where TStream : Stream
        where TList : IReadOnlyList<ColumnSynthesizer>
    {
        var columnCount = synthesizers.Count;
        stream.WriteValue(columnCount);
        // Reducing heap allocation
        for (var i = 0; i < columnCount; i++)
        {
            var synth = synthesizers[i];
            stream.WriteValue(synth.Resolver.Type.Value);
            stream.WriteValue(synth.Resolver.ColumnName);
        }
    }
}