using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace Astra.Engine;

using SynthesizersRead = (IIndexer.IIndexerReadHandler? handler, IColumnResolver resolver);
using SynthesizersWrite = (IIndexer.IIndexerWriteHandler? handler, IColumnResolver resolver);

public class WriteOperationsNotAllowed(string? msg = null) : Exception(msg);


public abstract class AbstractRegistryDump
{
    public abstract Stream PrepareStream();
    public abstract void CloseStream(Stream stream);
    public abstract bool CanBeDumped { get; }

    private sealed class EmptyDump : AbstractRegistryDump
    {
        public override Stream PrepareStream()
        {
            throw new NotSupportedException();
        }

        public override void CloseStream(Stream stream)
        {
            throw new NotSupportedException();
        }

        public override bool CanBeDumped => false;
    }

    public static AbstractRegistryDump Empty => new EmptyDump();
}

public sealed class DataIndexRegistry : IDisposable
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

    private readonly struct AutoReadLock(DataIndexRegistry registry, ILogger<AutoReadLock> logger) : IIndexersLock
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

    private static readonly ThreadLocal<ReadOnlyBufferStream?> LocalBuffer = new();
    private static readonly ThreadLocal<RecyclableMemoryStream?> LocalBulkInsertStream = new();
    
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DataIndexRegistry> _logger;
    private readonly ILogger<IndexersWriteLock> _writeLogger;
    private readonly ILogger<AutoReadLock> _readLogger;
    private readonly int _rowSize;
    private readonly int _hashSize;
    private readonly AbstractRegistryDump _dump;
    private readonly AutoIndexer _autoIndexer = new();
    private readonly ColumnSynthesizer[] _synthesizers;
    private readonly List<IDestructibleColumnResolver> _destructibleColumnResolvers = new();

    public int ColumnCount => _synthesizers.Length;
    public int IndexedColumnCount => _synthesizers.Length;
    public int ReferenceTypeColumnCount => _destructibleColumnResolvers.Count;

    public int RowsCount
    {
        get
        {
            using var autoIndexerLock = _autoIndexer.Read();
            return autoIndexerLock.Count;
        }
    }
    public DataIndexRegistry(SchemaSpecifications schema, ILoggerFactory? loggerFactory = null, AbstractRegistryDump? dump = null)
    {
        if (loggerFactory == null)
        {
            _loggerFactory = LoggerFactory.Create(_ =>
            {

            });
            loggerFactory = _loggerFactory;
        }
        _logger = loggerFactory.CreateLogger<DataIndexRegistry>();
        _writeLogger = loggerFactory.CreateLogger<IndexersWriteLock>();
        _readLogger = loggerFactory.CreateLogger<AutoReadLock>();
        _dump = dump ?? AbstractRegistryDump.Empty;
        _synthesizers = new ColumnSynthesizer[schema.Columns.Length];
        var i = 0;
        var offset = 0;
        var hashSize = 0;
        foreach (var column in schema.Columns)
        {
            var shouldBeHashed = column.ShouldBeHashed ?? column.Indexed;
            string dataType;
            IColumnResolver resolver;
            IIndexer? indexer;
            switch (column.DataType)
            {
                case DataType.DWordMask:
                {
                    dataType = nameof(DataType.DWord);
                    var cResolver = new IntegerColumnResolver(offset, shouldBeHashed);
                    offset += cResolver.Occupying;
                    resolver = cResolver;
                    indexer = column.Indexed ? new IntegerIndexer(cResolver) : null;
                    break;
                }
                case DataType.StringMask:
                {
                    dataType = nameof(DataType.String);
                    var cResolver = new StringColumnResolver(offset, shouldBeHashed);
                    _destructibleColumnResolvers.Add(cResolver);
                    offset += cResolver.Occupying;
                    resolver = cResolver;
                    indexer = column.Indexed ? new StringIndexer(cResolver) : null;
                    break;
                }
                case DataType.BytesMask:
                {
                    dataType = nameof(DataType.Bytes);
                    var cResolver = new BytesColumnResolver(offset, shouldBeHashed);
                    _destructibleColumnResolvers.Add(cResolver);
                    offset += cResolver.Occupying;
                    resolver = cResolver;
                    indexer = column.Indexed ? new BytesIndexer(cResolver) : null;
                    break;
                }
                default:
                    throw new DataTypeNotSupportedException();
            }

            if (shouldBeHashed)
            {
                hashSize += resolver.HashSize;
            }
            _logger.LogDebug("Column {}: found type: {}, indexed: {}, should be hashed: {}",
                i, dataType, column.Indexed, shouldBeHashed);

            if (indexer != null && !column.Indexed)
            {
                _logger.LogWarning("Schema requires column {} to be indexed, but data type does not support indexing",
                    column.Name);
            }
            _synthesizers[i++] = ColumnSynthesizer.Create(indexer, resolver);
        }
        _rowSize = offset;
        _hashSize = hashSize;
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

    public IEnumerable<ImmutableDataRow> FetchAll()
    {
        using var reader = _autoIndexer.Read();
        using var enumerator = reader.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }
    
    public IEnumerable<T> Aggregate<T>(Stream predicateStream) where T : IAstraSerializable
    {
        var dataOut = MemoryStreamPool.Allocate();
        try
        {
            using var readLock = new AutoReadLock(this, _readLogger);
            predicateStream.AggregateStream(dataOut, readLock);
            dataOut.Position = 0;
        }
        catch (Exception)
        {
            dataOut.Dispose();
            throw;
        }
        return IAstraSerializable.DeserializeStream<T, RecyclableMemoryStream>(dataOut, false);
    }
    
    public IEnumerable<T> Aggregate<T>(ReadOnlyMemory<byte> predicateStream) where T : IAstraSerializable
    {
        var buffer = LocalBuffer.Value ?? new(); 
        buffer.Buffer = predicateStream;
        LocalBuffer.Value = buffer;
        var dataOut = MemoryStreamPool.Allocate();
        try
        {
            using var readLock = new AutoReadLock(this, _readLogger);
            buffer.AggregateStream(dataOut, readLock);
            dataOut.Position = 0;
        }
        catch (Exception)
        {
            dataOut.Dispose();
            throw;
        }
        return IAstraSerializable.DeserializeStream<T, RecyclableMemoryStream>(dataOut, false);
    }
    
    private static int ConditionalCountInternal<T>(Stream predicateStream, T indexersLock) where T : struct, DataIndexRegistry.IIndexersLock
    {
        var set = predicateStream.Aggregate(indexersLock);
        return set?.Count ?? 0;
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
            count = set.Count;
            foreach (var row in set)
            {
                autoIndexerLock.RemoveExact(row);
                foreach (var write in writeLock)
                {
                    write.handler?.RemoveExact(row);
                }
                row.SelectiveDispose(_destructibleColumnResolvers);
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
        var immutableDataRow = DataRow.CreateImmutable(reader, _synthesizers, _rowSize, _hashSize);
    
        try
        {
            if (autoIndexerLock.Contains(immutableDataRow))
            {
                _logger.LogDebug("Row with hash '{}' existed", immutableDataRow.Hash);
                immutableDataRow.SelectiveDispose(_destructibleColumnResolvers);
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
            immutableDataRow.SelectiveDispose(_destructibleColumnResolvers);
            throw;
        }

        return true;
    }
    
    private int Insert(Stream reader, int rowCount, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var inserted = 0;
        for (var i = 0; i < rowCount; i++)
        {
            _ = InsertOne(reader, autoIndexerLock, writeLock) ? ++inserted : 0;
        }
        return inserted;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Insert(Stream reader, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var rowCount = reader.ReadInt();
        return Insert(reader, rowCount, autoIndexerLock, writeLock);
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
                var deleted = Clear(autoIndexerLock);
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

    public int Insert<T>(T value) where T : IAstraSerializable
    {
        using var inStream = MemoryStreamPool.Allocate();
        value.SerializeStream(new ForwardStreamWrapper(inStream));
        inStream.Position = 0;
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        return Insert(inStream, 1, autoIndexerLock, writeLock);
    }
    
    public int BulkInsert<T>(IEnumerable<T> values) where T : IAstraSerializable
    {
        var inStream = LocalBulkInsertStream.Value ?? MemoryStreamPool.Allocate();
        try
        {
            var count = 0;
            foreach (var value in values)
            {
                value.SerializeStream(new ForwardStreamWrapper(inStream));
                count++;
            }

            if (count == 0) return 0;
            inStream.Position = 0;
            using var autoIndexerLock = _autoIndexer.Write();
            using var writeLock = AcquireWriteLock();
            return Insert(inStream, count, autoIndexerLock, writeLock);
        }
        finally
        {
            if (inStream.Length > CommonProtocol.ThreadLocalStreamDisposalThreshold)
            {
                LocalBulkInsertStream.Value = null;
                inStream.Dispose();
            }
            else
            {
                inStream.SetLength(0);
                LocalBulkInsertStream.Value = inStream;
            }
        }
    }

    private int Clear(AutoIndexer.WriteHandler autoIndexerLock)
    {
        if (_destructibleColumnResolvers.Count == 0) return autoIndexerLock.Clear();
        var count = 0;
        foreach (var row in autoIndexerLock.ClearSequence())
        {
            foreach (var resolver in _destructibleColumnResolvers)
            {
                resolver.Destroy(row);
            }
            count++;
        }

        return count;
    }
    
    public int Clear()
    {
        using var autoIndexerLock = _autoIndexer.Write();
        var ret = Clear(autoIndexerLock);
        autoIndexerLock.Commit();
        return ret;
    }
}