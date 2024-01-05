using System.Collections;
using Microsoft.Extensions.Logging;

namespace Astra.Engine;

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
        public void Read(int index, Action<IIndexer.IIndexerReadHandler> action);
        public T Read<T>(int index, Func<IIndexer.IIndexerReadHandler, T> action);
    }
    public struct IndexersWriteLock(IIndexer.IIndexerWriteHandler[] handlers, ILogger<IndexersWriteLock> logger) : ITransaction, IEnumerable<IIndexer.IIndexerWriteHandler>, IIndexersLock
    {
        private bool _finalized = false;
        public void Dispose()
        {
            if (_finalized) return;
            _finalized = true;
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Dispose();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception occured while releasing writer lock");
                }
            }
            logger.LogDebug("Indexers' state released");
        }
        public IEnumerator<IIndexer.IIndexerWriteHandler> GetEnumerator()
        {
            return ((IEnumerable<IIndexer.IIndexerWriteHandler>)handlers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Commit()
        {
            if (_finalized) return;
            _finalized = true;
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Commit();
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
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Rollback();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception occured while rolling back writer lock");
                }
            }
            logger.LogDebug("Indexers' state rolled back");
        }

        public int Count { get; } = handlers.Length;

        public IIndexer.IIndexerReadHandler this[int index] => handlers[index];

        public void Read(int index, Action<IIndexer.IIndexerReadHandler> action)
            => action(handlers[index]);

        public T Read<T>(int index, Func<IIndexer.IIndexerReadHandler, T> action)
            => action(handlers[index]);
    }

    private readonly struct AutoReadLock(DataIndexRegistry registry, ILogger<AutoReadLock> logger) : IIndexersLock
    {
        public void Dispose()
        {
            
        }

        public void Read(int index, Action<IIndexer.IIndexerReadHandler> action)
        {
            using var indexerLock = registry._indexers[index].Read();
            action(indexerLock);
        }

        public T Read<T>(int index, Func<IIndexer.IIndexerReadHandler, T> action)
        {
            using var indexerLock = registry._indexers[index].Read();
            return action(indexerLock);
        }
    }
    
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DataIndexRegistry> _logger;
    private readonly ILogger<IndexersWriteLock> _writeLogger;
    private readonly ILogger<AutoReadLock> _readLogger;
    private readonly int _rowSize;
    private readonly int _hashSize;
    private readonly AbstractRegistryDump _dump;
    private readonly AutoIndexer _autoIndexer = new();
    private readonly List<IIndexer> _indexers = new();
    private readonly IColumnResolver[] _resolvers;
    private readonly List<IDestructibleColumnResolver> _destructibleColumnResolvers = new();

    public int ColumnCount => _resolvers.Length;
    public int IndexedColumnCount => _indexers.Count;
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
        _resolvers = new IColumnResolver[schema.Columns.Length];
        var i = 0;
        var offset = 0;
        var hashSize = 0;
        foreach (var column in schema.Columns)
        {
            var indexed = false;
            var shouldBeHashed = column.ShouldBeHashed ?? column.Indexed;
            string dataType;
            switch (column.DataType)
            {
                case DataType.DWordMask:
                {
                    dataType = nameof(DataType.DWord);
                    var resolver = new IntegerColumnResolver(offset, shouldBeHashed);
                    offset += resolver.Occupying;
                    _resolvers[i] = resolver;
                    if (column.Indexed)
                    {
                        _indexers.Add(new IntegerIndexer(resolver));
                        indexed = true;
                    }
                    break;
                }
                case DataType.StringMask:
                {
                    dataType = nameof(DataType.String);
                    var resolver = new StringColumnResolver(offset, shouldBeHashed);
                    _destructibleColumnResolvers.Add(resolver);
                    offset += resolver.Occupying;
                    _resolvers[i] = resolver;
                    if (column.Indexed)
                    {
                        _indexers.Add(new StringIndexer(resolver));
                        indexed = true;
                    }
                    break;
                }
                case DataType.BytesMask:
                {
                    dataType = nameof(DataType.Bytes);
                    var resolver = new BytesColumnResolver(offset, shouldBeHashed);
                    _destructibleColumnResolvers.Add(resolver);
                    offset += resolver.Occupying;
                    _resolvers[i] = resolver;
                    if (column.Indexed)
                    {
                        _indexers.Add(new BytesIndexer(resolver));
                        indexed = true;
                    }
                    break;
                }
                default:
                    throw new DataTypeNotSupportedException();
            }

            if (shouldBeHashed)
            {
                hashSize += _resolvers[i].HashSize;
            }
            _logger.LogDebug("Column {}: found type: {}, indexed: {}, should be hashed: {}",
                i, dataType, column.Indexed, shouldBeHashed);

            if (indexed && !column.Indexed)
            {
                _logger.LogWarning("Schema requires column {} to be indexed, but data type does not support indexing",
                    column.Name);
            }
            i++;
        }
        _rowSize = offset;
        _hashSize = hashSize;
        _logger.LogInformation("Row length: {} byte(s)", _rowSize);
        _logger.LogInformation("Hash stream length: {} byte(s)", _hashSize);
    }
    
    private IndexersWriteLock AcquireWriteLock()
    {
        var handlers = new IIndexer.IIndexerWriteHandler[_indexers.Count];
        var i = 0;
        foreach (var indexer in _indexers)
        {
            handlers[i++] = indexer.Write();
        }

        return new(handlers, _writeLogger);
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

    public HashSet<ImmutableDataRow>? Aggregate(Stream predicateStream)
    {
        return predicateStream.Aggregate(new AutoReadLock(this, _readLogger));
    }

    public int DeleteRows(Stream predicateStream)
    {
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        var set = predicateStream.Aggregate(writeLock);
        var count = 0;
        if (set != null)
        {
            count = set.Count;
            foreach (var row in set)
            {
                autoIndexerLock.RemoveExact(row);
                foreach (var indexer in writeLock)
                {
                    indexer.RemoveExact(row);
                }
                row.SelectiveDispose(_destructibleColumnResolvers);
            }
        }
        writeLock.Commit();
        autoIndexerLock.Commit();
        _logger.LogInformation("{} row(s) deleted", count);
        return count;
    }
    

    private void SerializeInternal<T>(Stream writer, T autoIndexerLock)
        where T : struct, IIndexer.IIndexerReadHandler
    {
        using var enumerator = autoIndexerLock.GetEnumerator();
        // Apparently using foreach here would box autoIndexerLock
        while (enumerator.MoveNext())
        {
            var row = enumerator.Current;
            foreach (var resolver in _resolvers)
            {
                resolver.Serialize(writer, row);
            }
        }
    }
    
    public void Serialize(Stream writer)
    {
        using var autoIndexerLock = _autoIndexer.Read();
        SerializeInternal(writer, autoIndexerLock);
    }

    /// <summary>
    /// Inserts a single row into the registry.
    /// </summary>
    /// <param name="reader">The <see cref="Stream"/> providing data for the new row.</param>
    /// <param name="autoIndexerLock">The <see cref="AutoIndexer.WriteHandler"/> used for managing unique indexes.</param>
    /// <param name="writeLock">The <see cref="IndexersWriteLock"/> used for managing write access to indexers.</param>
    /// <returns>
    /// <c>true</c> if the row was successfully inserted; otherwise, <c>false</c> if the row already existed.
    /// </returns>
    /// <remarks>
    /// This method reads data from the provided <paramref name="reader"/> and attempts to insert a new row into the data registry.
    /// If the row already exists in the <paramref name="autoIndexerLock"/> or an exception occurs during the insertion process,
    /// appropriate logging is performed, the newly created row is disposed, and the method returns <c>false</c>.
    /// </remarks>
    private bool InsertOne(Stream reader, AutoIndexer.WriteHandler autoIndexerLock, IndexersWriteLock writeLock)
    {
        var row = DataRow.Create(reader, _resolvers, _rowSize, _hashSize);
        var immutableDataRow = row.Consume(_resolvers);
    
        try
        {
            if (autoIndexerLock.Contains(immutableDataRow))
            {
                _logger.LogDebug("Row with hash '{}' existed", immutableDataRow.Hash);
                immutableDataRow.SelectiveDispose(_destructibleColumnResolvers);
                return false;
            }
            autoIndexerLock.Add(immutableDataRow);
            foreach (var handler in writeLock)
            {
                handler.Add(immutableDataRow);
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
    public int Insert(Stream reader)
    {
        var rowCount = reader.ReadInt();
        var inserted = 0;
        using var autoIndexerLock = _autoIndexer.Write();
        using var writeLock = AcquireWriteLock();
        for (var i = 0; i < rowCount; i++)
        {
            _ = InsertOne(reader, autoIndexerLock, writeLock) ? ++inserted : 0;
        }
        writeLock.Commit();
        autoIndexerLock.Commit();
        return inserted;
    }

    private void ConsumeOnce<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream
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
                var result = Aggregate(dataIn);
                if (result == null)
                {
                    dataOut.WriteValue(0);
                    break;
                }
                dataOut.WriteValue(result.Count);
                foreach (var row in result)
                {
                    foreach (var resolver in _resolvers)
                    {
                        resolver.Serialize(dataOut, row);
                    }
                }
                break;
            }
            case Command.UnsortedInsert:
            {
                var inserted = Insert(dataIn);
                dataOut.WriteValue(inserted);
                break;
            }
            case Command.Delete:
            {
                var deleted = DeleteRows(dataIn);
                dataOut.WriteValue(deleted);
                break;
            }
            default:
                throw new CommandNotSupported($"Command code not found: {command}");
        }
    }
    
    // dataIn layout
    // [count[command[description]]]
    //  1     4       >= 0
    //
    // dataOut layout
    // [is_faulted][results]
    //  1           >= 0
    public void ConsumeStream<TIn, TOut>(TIn dataIn, TOut dataOut) where TIn : Stream where TOut : Stream
    {
        dataOut.WriteByte(0);
        var commandCount = dataIn.ReadUInt();
        for (var i = 0; i < commandCount; i++)
            ConsumeOnce(dataIn, dataOut);
    }
    
    public Task ConsumeStreamAsync(Stream dataIn, Stream dataOut)
    {
        throw new NotSupportedException();
    }
}