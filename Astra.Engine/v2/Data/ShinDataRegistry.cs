using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.v2.Codegen;
using Astra.Engine.v2.Indexers;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Data.Codegen;
using Astra.TypeErasure.Planners;
using Astra.TypeErasure.Planners.Physical;
using Microsoft.Extensions.Logging;

namespace Astra.Engine.v2.Data;

public class ShinDataRegistry : IRegistry<ShinDataRegistry>
{
    public readonly struct Readers : IDisposable
    {
        private readonly BaseIndexer.Reader?[] _readers;
        public readonly BaseIndexer.Reader AutoIndexerLock;
        private readonly int _length;

        public Span<BaseIndexer.Reader?> ReaderLocks => new(_readers, 0, _length);
        
        public Readers(ShinDataRegistry registry)
        {
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
        }
    }
    
    public readonly struct Writers : IDisposable
    {
        private readonly BaseIndexer.Writer?[] _writers;
        public readonly BaseIndexer.Writer AutoIndexerLock;
        private readonly int _length;

        public Span<BaseIndexer.Writer?> WriterLocks => new(_writers, 0, _length);
        
        public Writers(ShinDataRegistry registry)
        {
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
        }
    }

    public struct WrappedEnumerator<T> : IEnumerator<T>
    {
        private PreparedLocalEnumerator<FlexWrapper<T>> _enumerator;

        public WrappedEnumerator(PreparedLocalEnumerator<FlexWrapper<T>> enumerator)
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

    public readonly struct WrappedEnumerable<T>(PreparedLocalEnumerable<FlexWrapper<T>> enumerable) : IEnumerable<T>
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
        private RWLock.ReadLockInstance _lock;
        private PreparedLocalEnumerator<FlexWrapper<T>> _enumerator;
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
                    var set = _host._autoIndexer.Data;
                    _lock = _host._autoIndexer.Latch.Read();
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
            _enumerator.Dispose();
            _lock.Dispose();
            _lock = new();
            _stage = 0;
        }

        public T Current => _enumerator.Current.Target;

        object IEnumerator.Current => Current!;
    }

    private readonly ILogger<ShinDataRegistry> _logger;
    private readonly DatastoreContext _context;
    private readonly BaseIndexer?[] _indexers;
    private readonly AutoIndexer _autoIndexer;
    private readonly Writers _writers;
    private readonly Readers _readers;
    private readonly int _columnCount;
    private readonly int _indexedColumnCount;

    internal BaseIndexer?[] Indexers => _indexers;

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
        _context = new(new ColumnSchema[tableSpecification.Columns.Length]);
        _indexers = new BaseIndexer[_context.TableSchema.Length];
        var degree = tableSpecification.BinaryTreeDegree < BTreeMap.MinDegree
            ? DefaultBinaryTreeDegree
            : tableSpecification.BinaryTreeDegree;
        for (var i = 0; i < _context.TableSchema.Length; i++)
        {
            var specification = tableSpecification.Columns[i];
            bool isIndexed = false;
            if (specification.Indexer.Type != IndexerType.None)
            {
                isIndexed = true;
                indexed += 1;
            }
            var shouldBeHashed = specification.ShouldBeHashed ?? specification.Indexer.Type != IndexerType.None;
            var schema = new ColumnSchema(specification.DataType.AstraDataType(),
                specification.Name, shouldBeHashed, isIndexed, i, degree);
            _context.TableSchema[i] = schema;
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
        _writers = new(this);
        _readers = new(this);
    }
    
    public void Dispose()
    {
        Clear();
        _writers.Dispose();
        _readers.Dispose();
    }

    public static ShinDataRegistry Fabricate(RegistrySchemaSpecifications tableSpecification, ILoggerFactory? loggerFactory = null,
        AbstractRegistryDump? dump = null)
    {
        return new(tableSpecification, loggerFactory);
    }

    public int RowsCount {
        get
        {
            using var dummy = _autoIndexer.Read();
            return dummy.Count;
        }
    }

    private bool InsertRow(DataRow row, ref readonly Writers writers)
    {
        try
        {
            if (!_autoIndexer.SynchronizedInsert(row, writers.WriterLocks))
            {
                _logger.LogDebug("Row with ID '{}' existed", row.GetHashCode());
                return false;
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

    private bool InsertOne(Stream reader, ref readonly Writers writers)
    {
        var row = DataRow.Deserialize(_context, reader);
        return InsertRow(row, in writers);
    }

    private bool InsertOne<T>(T data, ref readonly Writers writers) where T : ICellsSerializable
    {
        var row = DataRow.Create(_context, data);
        return InsertRow(row, in writers);
    }
    
    private int Insert(Stream reader, ref readonly Writers writers)
    {
        var flag = reader.ReadByte();
        var inserted = 0;
        while (flag != CommonProtocol.EndOfSetFlag && flag != -1)
        {
            _ = InsertOne(reader, in writers) ? ++inserted : 0;
            flag = reader.ReadByte();
        }
        return inserted;
    }
    
    private static readonly ThreadLocal<ReadOnlyBufferStream?> LocalBuffer = new();

    public PreparedLocalEnumerable<T> AggregateCompat<T>(Stream predicateStream) where T : ICellsSerializable
    {
        var span = _readers.ReaderLocks;
        return predicateStream.LocalAggregate<T, BaseIndexer.Reader>(ref span);
    }
    
    public PreparedLocalEnumerable<T> AggregateCompat<T>(ref readonly PhysicalPlan plan) where T : ICellsSerializable
    {
        var span = _readers.ReaderLocks;
        return plan.LocalAggregate<T, BaseIndexer.Reader>(ref span);
    }
    
    public PreparedLocalEnumerable<T> AggregateCompat<T>(ref readonly CompiledPhysicalPlan plan) where T : ICellsSerializable
    {
        if (plan.Host != this) throw new InvalidOperationException("This physical plan was compiled by a different registry");
        var span = _readers.ReaderLocks;
        return plan.LocalAggregate<T, BaseIndexer.Reader>(span);
    }
    
    public PreparedLocalEnumerable<T> AggregateCompat<T>(ReadOnlyMemory<byte> predicateStream) where T : ICellsSerializable
    {
        var stream = LocalBuffer.Value ?? new();
        LocalBuffer.Value = null;
        stream.Buffer = predicateStream;
        try
        {
            var span = _readers.ReaderLocks;
            return stream.LocalAggregate<T, BaseIndexer.Reader>(ref span);
        }
        finally
        {
            LocalBuffer.Value = stream;
        }
    }
    
    public WrappedEnumerable<T> Aggregate<T>(Stream predicateStream)
    {
        return new(AggregateCompat<FlexWrapper<T>>(predicateStream));
    }
    
    public WrappedEnumerable<T> Aggregate<T>(ref readonly PhysicalPlan plan)
    {
        return new(AggregateCompat<FlexWrapper<T>>(in plan));
    }
    
    public WrappedEnumerable<T> Aggregate<T>(ref readonly CompiledPhysicalPlan plan)
    {
        return new(AggregateCompat<FlexWrapper<T>>(in plan));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WrappedEnumerable<T> Aggregate<T>(PhysicalPlan plan) => Aggregate<T>(ref plan);
    
    public WrappedEnumerable<T> Aggregate<T>(ReadOnlyMemory<byte> predicateStream)
    {
        return new(AggregateCompat<FlexWrapper<T>>(predicateStream));
    }

    private int Delete(Stream predicateStream, ref readonly Writers writerLocks)
    {
        var span = writerLocks.WriterLocks;
        var result = predicateStream.Aggregate(ref span);
        switch (result)
        {
            case null:
                return 0;
            case HashSet<DataRow> set: // Reduce heap allocation
            {
                return _autoIndexer.SynchronizedRemove(set.GetEnumerator(), writerLocks.WriterLocks);
            }
            default:
            {
                return _autoIndexer.SynchronizedRemove(result.GetEnumerator(), writerLocks.WriterLocks);
            }
        }
    }
    
    public int Delete(Stream predicateStream)
    {
        return Delete(predicateStream, in _writers);
    }

    public bool InsertCompat<T>(T value) where T : ICellsSerializable
    {
        return InsertOne(value, in _writers);
    }
    
    public int BulkInsertCompat<T>(IEnumerable<T> values) where T : ICellsSerializable
    {
        var count = 0;
        foreach (var value in values)
        {
            if (InsertOne(value, in _writers)) count++;
        }

        return count;
    }

    public bool Insert<T>(T value) => InsertCompat(new FlexWrapper<T> { Target = value });

    public int BulkInsert<T>(IEnumerable<T> values) =>
        BulkInsertCompat(from value in values select new FlexWrapper<T> { Target = value });

    private int Clear(ref readonly Writers writerLocks)
    {
        return _autoIndexer.SynchronizedClear(writerLocks.WriterLocks);
    }
    
    public int Clear()
    {
        return Clear(in _writers);
    }

    IEnumerable<T> IRegistry.Aggregate<T>(ReadOnlyMemory<byte> predicate)
    {
        return Aggregate<T>(predicate);
    }
    
    IEnumerable<T> IRegistry.Aggregate<T>(PhysicalPlan plan)
    {
        return Aggregate<T>(plan);
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
                OutputLayout(dataOut,  _context.TableSchema);
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
            case Command.ReversedPlanAggregate:
            {
                var plan = new ReversedPhysicalPlanEnumerable(dataIn,  _context.TableSchema);
                OutputLayout(dataOut, _context.TableSchema);
                plan.Aggregate(dataOut, ref span);
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
                OutputLayout(dataOut,  _context.TableSchema);
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
            case Command.ReversedPlanAggregate:
            {
                var plan = new ReversedPhysicalPlanEnumerable(dataIn,  _context.TableSchema);
                OutputLayout(dataOut,  _context.TableSchema);
                plan.Aggregate(dataOut, ref span);
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
            for (var i = 0U; i < commandCount; i++)
                WriteOnce(dataIn, dataOut, in _writers);
            return;
        }

        for (var i = 0U; i < commandCount; i++)
        {
            ReadOnce(dataIn, dataOut, in _readers);
        }
    }

    public CompiledPhysicalPlan Compile(PhysicalPlan plan)
    {
        var executor = PlanCompiler.Compile(ref plan, this);
        return new(plan, executor, this);
    }
}