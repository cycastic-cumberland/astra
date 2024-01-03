using System.Collections;

namespace Astra.Engine;

public sealed class BytesIndexer(BytesColumnResolver resolver) :
    IIndexer<BytesIndexer.ReadHandler, BytesIndexer.WriteHandler>,
    IPointIndexer<BytesIndexer.ReadHandler, BytesIndexer.WriteHandler>,
    IPointIndexer<BytesCluster, BytesIndexer.ReadHandler, BytesIndexer.WriteHandler>
{
    private class Storage(BytesColumnResolver resolver)
    {
        public BytesColumnResolver Resolver => resolver;
        public Dictionary<Hash128, HashSet<ImmutableDataRow>> Data { get; } = new();
        public RWLock Lock { get; } = RWLock.Create();
    }

    public readonly struct ReadHandler(BytesIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<BytesCluster, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();
        
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.Bytes);
            using var value = predicateStream.ReadCluster();
            return CollectExact(value);
        }
        public HashSet<ImmutableDataRow>? CollectExact(BytesCluster match)
        {
            var hash = Hash128.HashMd5(match.Reader);
            Repository.Data.TryGetValue(hash, out var set);
            return set;
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            foreach (var (_, set) in Repository.Data)
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            var (_, hash) = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(hash, out var set) && set.Contains(row);
        }
        
        public HashSet<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            var op = predicateStream.ReadUInt();
            return op switch
            {
                Operation.Equal => CollectExact(predicateStream),
                _ => throw new OperationNotSupported($"Operation not supported: {op}")
            };
        }
        
        public void Dispose()
        {
            _readLock.Dispose();
        }
    }

    public readonly struct WriteHandler(BytesIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerWriteHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IPointIndexer<BytesCluster, ReadHandler, WriteHandler>.IPointIndexerWriteHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();
        
        public void Dispose()
        {
            _writeLock.Dispose();
        }
        
        public void Commit()
        {
            
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.Bytes);
            using var value = predicateStream.ReadCluster();
            return CollectExact(value);
        }
        public HashSet<ImmutableDataRow>? CollectExact(BytesCluster match)
        {
            var hash = Hash128.HashMd5(match.Reader);
            Repository.Data.TryGetValue(hash, out var set);
            return set;
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            var (_, hash) = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(hash, out var set) && set.Contains(row);
        }
        
        public HashSet<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            var op = predicateStream.ReadUInt();
            return op switch
            {
                Operation.Equal => CollectExact(predicateStream),
                _ => throw new OperationNotSupported($"Operation not supported: {op}")
            };
        }

        public void Add(ImmutableDataRow row)
        {
            var (_, hash) = Repository.Resolver.Dump(row);
            if (!Repository.Data.TryGetValue(hash, out var set))
            {
                set = new();
                Repository.Data[hash] = set;
            }

            set.Add(row);
        }

        public HashSet<ImmutableDataRow>? Remove(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.Bytes);
            var value = predicateStream.ReadCluster();
            return Remove(value);
        }

        public bool RemoveExact(ImmutableDataRow row)
        {
            var (_, hash) = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(hash, out var set) && set.Remove(row);
        }

        public HashSet<ImmutableDataRow>? Remove(BytesCluster match)
        {
            var hash = Hash128.HashMd5(match.Reader);
            return !Repository.Data.Remove(hash, out var set) ? null : set;
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            foreach (var (_, set) in Repository.Data)
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly Storage _storage = new(resolver);

    public ReadHandler Read() => new(this);
    IPointIndexer<BytesCluster>.IPointIndexerWriteHandler IPointIndexer<BytesCluster>.Write()
    {
        return Write();
    }

    IPointIndexer<BytesCluster>.IPointIndexerReadHandler IPointIndexer<BytesCluster>.Read()
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

    IIndexer.IIndexerReadHandler IIndexer.Read()
    {
        return Read();
    }

    public WriteHandler Write() => new(this);
}