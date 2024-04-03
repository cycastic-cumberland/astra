using System.Collections;
using System.Linq.Expressions;
using Astra.Common.Data;
using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public sealed class AutoIndexer : 
    IIndexer<AutoIndexer.ReadHandler, AutoIndexer.WriteHandler>
{
    private class Storage
    {
        public HashSet<ImmutableDataRow> Data { get; } = new();
        public RWLock Lock { get; } = RWLock.Create();
    }
    
    public readonly struct ReadHandler(AutoIndexer indexer) :
        IIndexer.IIndexerReadHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();
        
        public void Dispose()
        {
            _readLock.Dispose();
        }

        public int Count => Repository.Data.Count;
        
        public bool Contains(ImmutableDataRow row) => Repository.Data.Contains(row);
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return ((IEnumerable<ImmutableDataRow>)Repository.Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public IEnumerable<ImmutableDataRow> Fetch(Stream predicateStream)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Expression expression)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream)
        {
            throw new NotSupportedException();
        }
        
        public HashSet<ImmutableDataRow> FetchAllUnsafe()
        {
            // ReSharper disable once NotDisposedResourceIsReturned
            return Repository.Data;
        }
    }

    public struct WriteHandler(AutoIndexer indexer) :
        IIndexer.IIndexerWriteHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();

        private bool _finished;
        
        public void Dispose()
        {
            if (_finished) return;
            _finished = true;
            _writeLock.Dispose();
        }
        
        public int Count => Repository.Data.Count;
        
        public bool Contains(ImmutableDataRow row) => Repository.Data.Contains(row);

        public void Add(ImmutableDataRow row)
        {
            Repository.Data.Add(row);
        }

        public bool RemoveExact(ImmutableDataRow row)
        {
            var hadSomething = Contains(row);
            return hadSomething && Repository.Data.Remove(row);
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return ((IEnumerable<ImmutableDataRow>)Repository.Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<ImmutableDataRow> ClearSequence()
        {
            foreach (var row in Repository.Data)
            {
                yield return row;
            }
            Repository.Data.Clear();
        }

        public int Clear()
        {
            var deleted = Repository.Data.Count;
            Repository.Data.Clear();
            return deleted;
        }

        void IIndexer.IIndexerWriteHandler.Clear()
        {
            _ = Clear();
        }
        
        public void Commit()
        {
            Dispose();
        }

        public void Rollback()
        {
            Dispose();
        }
        
        public IEnumerable<ImmutableDataRow> Fetch(Stream predicateStream)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Expression expression)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream)
        {
            throw new NotSupportedException();
        }
        
        public HashSet<ImmutableDataRow> FetchAllUnsafe()
        {
            // ReSharper disable once NotDisposedResourceIsReturned
            return Repository.Data;
        }
    }

    private readonly Storage _storage = new();

    public ReadHandler Read() => new(this);
    IIndexer.IIndexerWriteHandler IIndexer.Write()
    {
        return Write();
    }

    public FeaturesList SupportedReadOperations => FeaturesList.None;
    public FeaturesList SupportedWriteOperations => FeaturesList.None;
    public uint Priority => 0;
    public DataType Type => DataType.None;

    IIndexer.IIndexerReadHandler IIndexer.Read()
    {
        return Read();
    }

    public WriteHandler Write() => new(this);
}