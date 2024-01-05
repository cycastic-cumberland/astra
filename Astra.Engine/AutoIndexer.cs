using System.Collections;
using System.Runtime.InteropServices;

namespace Astra.Engine;

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
        
        public HashSet<ImmutableDataRow> Fetch(Stream predicateStream)
        {
            throw new NotSupportedException();
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
            if (!hadSomething) return false;
            return Repository.Data.Remove(row);
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return ((IEnumerable<ImmutableDataRow>)Repository.Data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public void Commit()
        {
            Dispose();
        }

        public void Rollback()
        {
            Dispose();
        }
        
        public HashSet<ImmutableDataRow> Fetch(Stream predicateStream)
        {
            throw new NotSupportedException();
        }
    }

    private readonly Storage _storage = new();

    public ReadHandler Read() => new(this);
    IIndexer.IIndexerWriteHandler IIndexer.Write()
    {
        return Write();
    }

    IIndexer.IIndexerReadHandler IIndexer.Read()
    {
        return Read();
    }

    public WriteHandler Write() => new(this);
}