using System.Collections;

namespace Astra.Engine;

public sealed class IntegerIndexer(IntegerColumnResolver resolver) :
    IIndexer<IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>,
    IPointIndexer<IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>,
    IPointIndexer<int, IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>
{
    private class Storage(IntegerColumnResolver resolver)
    {
        public IntegerColumnResolver Resolver => resolver;
        public Dictionary<int, HashSet<ImmutableDataRow>> Data { get; } = new();
        public RWLock Lock { get; } = RWLock.Create();
    }

    public readonly struct ReadHandler(IntegerIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<int, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var value = predicateStream.ReadInt();
            return CollectExact(value);
        }

        public HashSet<ImmutableDataRow>? CollectExact(int match)
        {
            Repository.Data.TryGetValue(match, out var set);
            return set;
        }
        
        public void Dispose()
        {
            _readLock.Dispose();
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

        public bool Contains(ImmutableDataRow row)
        {
            var index = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(index, out var set) && set.Contains(row);
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct WriteHandler(IntegerIndexer indexer) : 
        IIndexer<ReadHandler, WriteHandler>.IIndexerWriteHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IPointIndexer<int, ReadHandler, WriteHandler>.IPointIndexerWriteHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();

        public void Dispose()
        {
            Rollback();
            _writeLock.Dispose();
        }
        
        public void Commit()
        {
        }

        public void Rollback()
        {
            
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var value = predicateStream.ReadInt();
            return CollectExact(value);
        }

        public HashSet<ImmutableDataRow>? CollectExact(int match)
        {
            Repository.Data.TryGetValue(match, out var set);
            return set;
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            var index = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(index, out var set) && set.Contains(row);
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
            var index = Repository.Resolver.Dump(row);
            if (!Repository.Data.TryGetValue(index, out var set))
            {
                set = new();
                Repository.Data[index] = set;
            }

            set.Add(row);
        }

        public HashSet<ImmutableDataRow>? Remove(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var value = predicateStream.ReadInt();
            return Remove(value);
        }
        
        public bool RemoveExact(ImmutableDataRow row)
        {
            var index = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(index, out var set) && set.Remove(row);
        }

        public HashSet<ImmutableDataRow>? Remove(int match)
        {
            return !Repository.Data.Remove(match, out var set) ? null : set;
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
    IPointIndexer<int>.IPointIndexerWriteHandler IPointIndexer<int>.Write()
    {
        return Write();
    }

    IPointIndexer<int>.IPointIndexerReadHandler IPointIndexer<int>.Read()
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