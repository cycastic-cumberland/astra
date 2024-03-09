using System.Collections;
using Astra.Collections.Recyclable;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public abstract class SelfContainedIndexer<T, TIndexer, TRead, TWrite> :
    IIndexer<SelfContainedIndexer<T, TIndexer, TRead, TWrite>.ReadHandler, SelfContainedIndexer<T, TIndexer, TRead, TWrite>.WriteHandler>,
    IPointIndexer<SelfContainedIndexer<T, TIndexer, TRead, TWrite>.ReadHandler, SelfContainedIndexer<T, TIndexer, TRead, TWrite>.WriteHandler>,
    IPointIndexer<T, SelfContainedIndexer<T, TIndexer, TRead, TWrite>.ReadHandler, SelfContainedIndexer<T, TIndexer, TRead, TWrite>.WriteHandler>
    where TIndexer : IPointIndexer<T, TRead, TWrite>
    where TRead : IIndexer.IIndexerReadHandler, IPointIndexer.IPointIndexerReadHandler, IPointIndexer<T>.IPointIndexerReadHandler
    where TWrite : IIndexer.IIndexerWriteHandler, IPointIndexer.IPointIndexerWriteHandler, IPointIndexer<T>.IPointIndexerWriteHandler
    where T : notnull
{
    private readonly TIndexer _indexer;
    private readonly RWLock _rwLock;
    private readonly ConcurrentObjectPool<ReadHandler, ReadHandlerFactory> _readPool;
    private readonly ConcurrentObjectPool<WriteHandler, WriteHandlerFactory> _writePool;

    public class ReadHandler(SelfContainedIndexer<T, TIndexer, TRead, TWrite> host) :
        IIndexer.IIndexerReadHandler,
        IPointIndexer.IPointIndexerReadHandler,
        IPointIndexer<T, ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IRecyclable
    {
        private RWLock.ReadLockInstance _readLock;
        private readonly TRead _indexer = host._indexer.Read();

        public void Activate()
        {
            _readLock = host._rwLock.Read();
        }
        
        public HashSet<ImmutableDataRow>? CollectExact(T match)
        {
            return _indexer.CollectExact(match);
        }

        public void Dispose()
        {
            _readLock.Dispose();
            _readLock = default;
            host._readPool.Return(this);
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _indexer.CollectExact(predicateStream);
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _indexer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_indexer).GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _indexer.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _indexer.Fetch(predicateStream);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream)
        {
            return _indexer.Fetch(operation, predicateStream);
        }

        public void Reset()
        {
            
        }
    }

    public class WriteHandler(SelfContainedIndexer<T, TIndexer, TRead, TWrite> host) :
        IIndexer.IIndexerWriteHandler,
        IPointIndexer.IPointIndexerWriteHandler,
        IPointIndexer<T, ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IRecyclable
    {
        private RWLock.ReadLockInstance _writeLock;
        private readonly TWrite _indexer = host._indexer.Write();
        private bool _finished;

        public void Activate()
        {
            _writeLock = host._rwLock.Read();
            _finished = false;
        }
        
        public HashSet<ImmutableDataRow>? CollectExact(T match)
        {
            return _indexer.CollectExact(match);
        }

        public HashSet<ImmutableDataRow>? Remove(T match)
        {
            return _indexer.Remove(match);
        }

        public void Dispose()
        {
            if (_finished) return;
            _finished = true;
            _writeLock.Dispose();
            _writeLock = default;
            host._writePool.Return(this);
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _indexer.CollectExact(predicateStream);
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _indexer.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_indexer).GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _indexer.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _indexer.Fetch(predicateStream);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream)
        {
            return _indexer.Fetch(operation, predicateStream);
        }

        public void Commit()
        {
            _indexer.Commit();
            Dispose();
        }

        public void Rollback()
        {
            _indexer.Rollback();
            Dispose();
        }

        void IIndexer.IIndexerWriteHandler.Add(ImmutableDataRow row)
        {
            ((IIndexer.IIndexerWriteHandler)_indexer).Add(row);
        }

        public HashSet<ImmutableDataRow>? Remove(Stream predicateStream)
        {
            return _indexer.Remove(predicateStream);
        }

        bool IPointIndexer.IPointIndexerWriteHandler.RemoveExact(ImmutableDataRow row)
        {
            return ((IPointIndexer.IPointIndexerWriteHandler)_indexer).RemoveExact(row);
        }

        void IPointIndexer.IPointIndexerWriteHandler.Add(ImmutableDataRow row)
        {
            ((IPointIndexer.IPointIndexerWriteHandler)_indexer).Add(row);
        }

        bool IIndexer.IIndexerWriteHandler.RemoveExact(ImmutableDataRow row)
        {
            return ((IIndexer.IIndexerWriteHandler)_indexer).RemoveExact(row);
        }

        public void Clear()
        {
            _indexer.Clear();
        }

        public void Reset()
        {
            
        }
    }

    private readonly struct ReadHandlerFactory(SelfContainedIndexer<T, TIndexer, TRead, TWrite> host)
        : IRecyclableFactory<ReadHandler>
    {
        public ReadHandler Create()
        {
            return new(host);
        }
    }
    
    private readonly struct WriteHandlerFactory(SelfContainedIndexer<T, TIndexer, TRead, TWrite> host)
        : IRecyclableFactory<WriteHandler>
    {
        public WriteHandler Create()
        {
            return new(host);
        }
    }

    protected SelfContainedIndexer(TIndexer indexer)
    {
        _indexer = indexer;
        _rwLock = RWLock.Create();
        _readPool = new(new(this));
        _writePool = new(new(this));
    }

    public ReadHandler Read()
    {
        var item = _readPool.Retrieve();
        item.Activate();
        return item;
    }
    IPointIndexer<T>.IPointIndexerWriteHandler IPointIndexer<T>.Write()
    {
        return Write();
    }

    IPointIndexer<T>.IPointIndexerReadHandler IPointIndexer<T>.Read()
    {
        return Read();
    }

    IPointIndexer.IPointIndexerWriteHandler IPointIndexer.Write()
    {
        return Write();
    }

    IPointIndexer.IPointIndexerReadHandler IPointIndexer.Read()
    {
        return Read();
    }

    IIndexer.IIndexerWriteHandler IIndexer.Write()
    {
        return Write();
    }

    public FeaturesList SupportedReadOperations => _indexer.SupportedReadOperations;
    public FeaturesList SupportedWriteOperations => _indexer.SupportedWriteOperations;
    public uint Priority => _indexer.Priority;
    public DataType Type => _indexer.Type;

    IIndexer.IIndexerReadHandler IIndexer.Read()
    {
        return Read();
    }

    public WriteHandler Write()
    {
        var item = _writePool.Retrieve();
        item.Activate();
        return item;
    }
}