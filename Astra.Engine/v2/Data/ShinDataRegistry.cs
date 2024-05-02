using System.Buffers;
using System.Collections;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.v2.Indexers;
using Microsoft.Extensions.Logging;

namespace Astra.Engine.v2.Data;

public class ShinDataRegistry : IRegistry<ShinDataRegistry>
{
    public readonly struct Readers : IDisposable
    {
        private readonly RWLock.ReadLockInstance _readLock;
        private readonly BaseIndexer.Reader?[] _readers;
        public readonly BaseIndexer.Reader AutoIndexerLock;
        private readonly int _length;

        public Span<BaseIndexer.Reader?> ReaderLocks => new(_readers, 0, _length);
        
        public Readers(ShinDataRegistry registry)
        {
            _readLock = registry._globalLock.Read();
            AutoIndexerLock = registry._autoIndexer.Read();
            _length = registry._indexers.Length;
            _readers = ArrayPool<BaseIndexer.Reader?>.Shared.Rent(_length);
            for (var i = 0; i < _length; i++)
            {
                _readers[i] = registry._indexers[i]?.Read();
            }
        }
        public void Dispose()
        {
            for (var i = 0; i < ReaderLocks.Length; i++)
            {
                ref var writer = ref ReaderLocks[i];
                writer?.Dispose();
            }
            ArrayPool<BaseIndexer.Reader?>.Shared.Return(_readers);
            AutoIndexerLock.Dispose();
            _readLock.Dispose();
        }
    }
    
    public readonly struct Writers : IDisposable
    {
        private readonly RWLock.WriteLockInstance _writeLock;
        private readonly BaseIndexer.Writer?[] _writers;
        public readonly BaseIndexer.Writer AutoIndexerLock;
        private readonly int _length;

        public Span<BaseIndexer.Writer?> WriterLocks => new(_writers, 0, _length);
        
        public Writers(ShinDataRegistry registry)
        {
            _writeLock = registry._globalLock.Write();
            AutoIndexerLock = registry._autoIndexer.Write();
            _length = registry._indexers.Length;
            _writers = ArrayPool<BaseIndexer.Writer?>.Shared.Rent(_length);
            for (var i = 0; i < _length; i++)
            {
                _writers[i] = registry._indexers[i]?.Write();
            }
        }
        public void Dispose()
        {
            for (var i = 0; i < WriterLocks.Length; i++)
            {
                ref var writer = ref WriterLocks[i];
                writer?.Dispose();
            }
            ArrayPool<BaseIndexer.Writer?>.Shared.Return(_writers);
            AutoIndexerLock.Dispose();
            _writeLock.Dispose();
        }
    }

    public struct WrappedEnumerator<T> : IEnumerator<T>
    {
        private PreparedLocalEnumerator<FlexSerializable<T>> _enumerator;

        public WrappedEnumerator(PreparedLocalEnumerator<FlexSerializable<T>> enumerator)
        {
            _enumerator = enumerator;
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

    public readonly struct WrappedEnumerable<T>(PreparedLocalEnumerable<FlexSerializable<T>> enumerable) : IEnumerable<T>
    {
        public WrappedEnumerator<T> GetEnumerator()
        {
            return new(enumerable.GetEnumerator());
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
        private readonly ShinDataRegistry _host;
        private Readers _readers;
        private PreparedLocalEnumerator<FlexSerializable<T>> _enumerator;
        private uint _stage;

        public Enumerator(ShinDataRegistry host)
        {
            _host = host;
        }
        
        public void Dispose()
        {
            Reset();
        }

        public bool MoveNext()
        {
            switch (_stage)
            {
                case 0:
                {
                    _readers = _host.AcquireReadLocks();
                    var set = _host._autoIndexer.Probe();
                    _enumerator = new(set);
                    _stage = 1;
                    goto case 1;
                }
                case 1:
                {
                    if (_enumerator.MoveNext()) return true;
                    Reset();
                    _stage = 2;
                    goto default;
                }
                default:
                {
                    return false;
                }
            }
        }

        public void Reset()
        {
            if (_stage == 0) return;
            _readers.Dispose();
            _enumerator.Dispose();
            _stage = 0;
        }

        public T Current => _enumerator.Current.Target;

        object IEnumerator.Current => Current!;
    }

    private readonly ILogger<ShinDataRegistry> _logger;
    private readonly ColumnSchema[] _tableSchema;
    private readonly BaseIndexer?[] _indexers;
    private readonly AutoIndexer _autoIndexer;
    private readonly RWLock _globalLock = RWLock.Create();
    private readonly int _columnCount;
    private readonly int _indexedColumnCount;

    public const int DefaultBinaryTreeDegree = 100;
    public int ColumnCount => _columnCount;
    public int IndexedColumnCount => _indexedColumnCount;
    
    public ShinDataRegistry(RegistrySchemaSpecifications tableSpecification, ILoggerFactory? loggerFactory = null)
    {
        _columnCount = tableSpecification.Columns.Length;
        var indexed = 0;
        _autoIndexer = new();
        loggerFactory ??= LoggerFactory.Create(_ => { });
        _logger = loggerFactory.CreateLogger<ShinDataRegistry>();
        _tableSchema = new ColumnSchema[tableSpecification.Columns.Length];
        _indexers = new BaseIndexer[_tableSchema.Length];
        var degree = tableSpecification.BinaryTreeDegree < BTreeMap.MinDegree
            ? DefaultBinaryTreeDegree
            : tableSpecification.BinaryTreeDegree;
        for (var i = 0; i < _tableSchema.Length; i++)
        {
            var specification = tableSpecification.Columns[i];
            if (specification.Indexer.Type != IndexerType.None) 
                indexed += 1;
            var shouldBeHashed = specification.ShouldBeHashed ?? specification.Indexer.Type != IndexerType.None;
            var schema = new ColumnSchema(specification.DataType.AstraDataType(),
                specification.Name, shouldBeHashed, i, degree);
            _tableSchema[i] = schema;
            _indexers[i] = specification.Indexer.Type switch
            {
                IndexerType.None => null,
                IndexerType.Generic => new GenericIndexer(schema),
                IndexerType.BTree => new NumericIndexer(schema),
                IndexerType.Fuzzy => throw new NotSupportedException(nameof(IndexerType.Fuzzy)),
                IndexerType.Dynamic => throw new NotSupportedException(nameof(IndexerType.Dynamic)),
                _ => throw new ArgumentOutOfRangeException()
            };
            _logger.LogDebug("Column {}: found type: {}, indexer: {}, should be hashed: {}",
                i, specification.DataType, specification.Indexer.Type, shouldBeHashed);
        }

        _indexedColumnCount = indexed;
    }
    
    private Readers AcquireReadLocks()
    {
        return new(this);
    }
    
    private Writers AcquireWriteLocks()
    {
        return new(this);
    }
    
    
    public void Dispose()
    {
        Clear();
    }

    public static ShinDataRegistry Fabricate(RegistrySchemaSpecifications tableSpecification, ILoggerFactory? loggerFactory = null,
        AbstractRegistryDump? dump = null)
    {
        return new(tableSpecification, loggerFactory);
    }

    public int RowsCount {
        get
        {
            using var readLock = _globalLock.Read();
            using var dummy = _autoIndexer.Read();
            return dummy.Count;
        }
    }

    private bool InsertOne(Stream reader, ref readonly Writers writers)
    {
        var row = DataRow.Deserialize(_tableSchema, reader);
        try
        {
            if (!writers.AutoIndexerLock.Add(row))
            {
                _logger.LogDebug("Row with hash '{}' existed", row.GetHashCode());
                row.Dispose();
                return false;
            }

            foreach (var writer in writers.WriterLocks)
            {
                writer?.Add(row);
            }
            
            _logger.LogDebug("Row inserted: {}", row.GetHashCode());
            return true;
        }
        catch (Exception e)
        {
            var hash = row.GetHashCode();
            _logger.LogError(e, "Exception caught while adding row '{}'", hash);
            row.Dispose();
            throw;
        }
    }
    
    private int Insert(Stream reader, ref readonly Writers writers)
    {
        var rowCount = reader.ReadInt();
        var inserted = 0;
        for (var i = 0; i < rowCount; i++)
        {
            _ = InsertOne(reader, in writers) ? ++inserted : 0;
        }
        return inserted;
    }
    
    private static readonly ThreadLocal<ReadOnlyBufferStream?> LocalBuffer = new();

    public PreparedLocalEnumerable<T> AggregateCompat<T>(Stream predicateStream) where T : IAstraSerializable
    {
        using var readLock = AcquireReadLocks();
        var span = readLock.ReaderLocks;
        return predicateStream.LocalAggregate<T, BaseIndexer.Reader>(ref span);
    }
    
    public PreparedLocalEnumerable<T> AggregateCompat<T>(ReadOnlyMemory<byte> predicateStream) where T : IAstraSerializable
    {
        var stream = LocalBuffer.Value ?? new();
        LocalBuffer.Value = null;
        stream.Buffer = predicateStream;
        try
        {
            using var readLock = AcquireReadLocks();
            var span = readLock.ReaderLocks;
            return stream.LocalAggregate<T, BaseIndexer.Reader>(ref span);
        }
        finally
        {
            LocalBuffer.Value = stream;
        }
    }
    
    public WrappedEnumerable<T> Aggregate<T>(Stream predicateStream)
    {
        return new(AggregateCompat<FlexSerializable<T>>(predicateStream));
    }
    
    public WrappedEnumerable<T> Aggregate<T>(ReadOnlyMemory<byte> predicateStream)
    {
        return new(AggregateCompat<FlexSerializable<T>>(predicateStream));
    }

    private static int Delete<T>(T enumerator, ref readonly Writers writerLocks) where T : IEnumerator<DataRow>
    {
        var i = 0;
        while (enumerator.MoveNext())
        {
            using var row = enumerator.Current;
            writerLocks.AutoIndexerLock.Remove(row);
            foreach (var writer in writerLocks.WriterLocks)
            {
                writer?.Remove(row);
            }
            i++;
        }

        return i;
    }

    private static int Delete(Stream predicateStream, ref readonly Writers writerLocks)
    {
        var span = writerLocks.WriterLocks;
        var result = predicateStream.Aggregate(ref span);
        switch (result)
        {
            case null:
                return 0;
            // Reduce heap allocation
            case HashSet<DataRow> set:
            {
                using var enumerator = set.GetEnumerator();
                return Delete(enumerator, in writerLocks);
            }
            default:
            {
                using var enumerator = result.GetEnumerator();
                return Delete(enumerator, in writerLocks);
            }
        }
    }
    
    public int Delete(Stream predicateStream)
    {
        using var writerLocks = AcquireWriteLocks();
        ref readonly var locksRef = ref writerLocks;
        return Delete(predicateStream, in locksRef);
    }

    public bool InsertCompat<T>(T value) where T : IAstraSerializable
    {
        using var inStream = MemoryStreamPool.Allocate();
        value.SerializeStream(new ForwardStreamWrapper(inStream));
        inStream.Position = 0;
        using var writerLocks = AcquireWriteLocks();
        ref readonly var locksRef = ref writerLocks;
        return InsertOne(inStream, in locksRef);
    }
    
    public int BulkInsertCompat<T>(IEnumerable<T> values) where T : IAstraSerializable
    {
        using var inStream = LocalStreamWrapper.Create();
        var wrapper = new ForwardStreamWrapper(inStream.LocalStream);
        var count = 0;
        using var writerLocks = AcquireWriteLocks();
        ref readonly var locksRef = ref writerLocks;
        foreach (var value in values)
        {
            inStream.LocalStream.SetLength(0);
            value.SerializeStream(wrapper);
            inStream.LocalStream.Position = 0;
            _ = InsertOne(inStream.LocalStream, in locksRef) ? count++ : 0;
        }
        return count;
    }

    public bool Insert<T>(T value) => InsertCompat(new FlexSerializable<T> { Target = value });

    public int BulkInsert<T>(IEnumerable<T> values) =>
        BulkInsertCompat(from value in values select new FlexSerializable<T> { Target = value });

    private int Clear(ref readonly Writers writerLocks)
    {
        var count = writerLocks.AutoIndexerLock.Count;
        foreach (var writer in writerLocks.WriterLocks)
        {
            writer?.Clear();
        }
        foreach (var row in writerLocks.AutoIndexerLock)
        {
            row.Dispose();
        }
        writerLocks.AutoIndexerLock.Clear();
        return count;
    }
    
    public int Clear()
    {
        using var writerLocks = AcquireWriteLocks();
        return Clear(in writerLocks);
    }

    IEnumerable<T> IRegistry.Aggregate<T>(ReadOnlyMemory<byte> predicate)
    {
        return Aggregate<T>(predicate);
    }

    IEnumerable<T> IRegistry.Aggregate<T>(Stream predicate)
    {
        return Aggregate<T>(predicate);
    }

    public Enumerator<T> GetEnumerator<T>()
    {
        return new(this);
    }

    IEnumerator<T> IRegistry.GetEnumerator<T>() => GetEnumerator<T>();

    private int ConditionalCount<T>(Stream predicateStream, ref readonly Span<T?> span) where T : struct, BaseIndexer.IReadable
    {
        var set = predicateStream.Aggregate(in span);
        return set?.Count() ?? 0;
    }
    
    private static void OutputLayout<TStream>(TStream stream, ColumnSchema[] tableSchema) 
        where TStream : Stream
    {
        var columnCount = tableSchema.Length;
        stream.WriteValue(columnCount);
        foreach (var schema in tableSchema)
        {
            stream.WriteValue(schema.Type.Value);
            stream.WriteValue(schema.ColumnName);
        }
    }
    
    private void ReadOnce<TIn, TOut>(TIn dataIn, TOut dataOut, in Readers locksRef) where TIn : Stream where TOut : Stream
    {
        var command = dataIn.ReadUInt();
        var span = locksRef.ReaderLocks;
        switch (command)
        {
            case Command.UnsortedAggregate:
            {
                OutputLayout(dataOut, _tableSchema);
                dataIn.Aggregate(dataOut, ref span);
                break;
            }
            case Command.CountAll:
            {
                dataOut.WriteValue(locksRef.AutoIndexerLock.Count);
                break;
            }
            case Command.ConditionalCount:
            {
                var count = ConditionalCount(dataIn, ref span);
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

    private void WriteOnce<TIn, TOut>(TIn dataIn, TOut dataOut, in Writers locksRef) where TIn : Stream where TOut : Stream
    {
        var command = dataIn.ReadUInt();
        var span = locksRef.WriterLocks;
        switch (command)
        {
            case Command.UnsortedAggregate:
            {
                OutputLayout(dataOut, _tableSchema);
                dataIn.Aggregate(dataOut, ref span);
                break;
            }
            case Command.CountAll:
            {
                dataOut.WriteValue(RowsCount);
                break;
            }
            case Command.ConditionalCount:
            {
                var count = ConditionalCount(dataIn, ref span);
                dataOut.WriteValue(count);
                break;
            }
            case Command.UnsortedInsert:
            {
                var inserted = Insert(dataIn, in locksRef);
                dataOut.WriteValue(inserted);
                break;
            }
            case Command.ConditionalDelete:
            {
                var deleted = Delete(dataIn, in locksRef);
                dataOut.WriteValue(deleted);
                break;
            }
            case Command.Clear:
            {
                var deleted = Clear(in locksRef);
                dataOut.WriteValue(deleted);
                break;
            }
            default:
                throw new CommandNotSupported($"Command code not found: {command}");
        }
    }
    

    public void ConsumeStream<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream
    {
        dataOut.WriteByte(0);
        var commandHeader = dataIn.ReadUInt();
        var (commandCount, enableWrite) = Command.SplitCommandHeader(commandHeader);
        if (enableWrite)
        {
            using var writerLocks = AcquireWriteLocks();
            ref readonly var locksRef = ref writerLocks;
            for (var i = 0U; i < commandCount; i++)
                WriteOnce(dataIn, dataOut, in locksRef);
            return;
        }

        for (var i = 0U; i < commandCount; i++)
        {
            using var readLocks = AcquireReadLocks();
            ref readonly var locksRef = ref readLocks;
            ReadOnce(dataIn, dataOut, in locksRef);
        }
    }
}