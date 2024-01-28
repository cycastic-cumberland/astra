using System.Collections;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common;

namespace Astra.Engine;

public sealed class IntegerIndexer(IntegerColumnResolver resolver, int degree) :
    IIndexer<IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>,
    IPointIndexer<IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>,
    IPointIndexer<int, IntegerIndexer.ReadHandler, IntegerIndexer.WriteHandler>
{
    private class Storage(IntegerColumnResolver resolver, int degree)
    {
        public IntegerColumnResolver Resolver => resolver;
        public BTreeMap<int, HashSet<ImmutableDataRow>> Data { get; } = new(degree);
        public RWLock Lock { get; } = RWLock.Create();
    }

    private readonly struct ReadHelper(Storage repository)  :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<int, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var value = predicateStream.ReadInt();
            return CollectExact(value);
        }

        public HashSet<ImmutableDataRow>? CollectExact(int match)
        {
            repository.Data.TryGetValue(match, out var set);
            return set;
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            foreach (var (_, set) in repository.Data)
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }

        public bool Contains(ImmutableDataRow row)
        {
            var index = repository.Resolver.Dump(row);
            return repository.Data.TryGetValue(index, out var set) && set.Contains(row);
        }

        private IEnumerable<ImmutableDataRow> ClosedBetween(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var left = predicateStream.ReadInt();
            var right = predicateStream.ReadInt();
            foreach (var (_, set) in repository.Data.Collect(left, right, CollectionMode.ClosedInterval))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> GreaterThan(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var left = predicateStream.ReadInt();
            foreach (var (_, set) in repository.Data.CollectFrom(left, false))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var left = predicateStream.ReadInt();
            foreach (var (_, set) in repository.Data.CollectFrom(left))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> LesserThan(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var left = predicateStream.ReadInt();
            foreach (var (_, set) in repository.Data.CollectTo(left, false))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> LesserOrEqualsTo(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.DWord);
            var left = predicateStream.ReadInt();
            foreach (var (_, set) in repository.Data.CollectTo(left))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            var op = predicateStream.ReadUInt();
            return op switch
            {
                Operation.Equal => CollectExact(predicateStream),
                Operation.ClosedBetween => ClosedBetween(predicateStream),
                Operation.GreaterThan => GreaterThan(predicateStream),
                Operation.GreaterOrEqualsTo => GreaterOrEqualsTo(predicateStream),
                Operation.LesserThan => LesserThan(predicateStream),
                Operation.LesserOrEqualsTo => LesserOrEqualsTo(predicateStream),
                _ => throw new OperationNotSupported($"Operation not supported: {op}")
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            
        }
    }

    public readonly struct ReadHandler(IntegerIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<int, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        private readonly ReadHelper _helper = new(indexer._storage);
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _helper.CollectExact(predicateStream);
        }

        public HashSet<ImmutableDataRow>? CollectExact(int match)
        {
            return _helper.CollectExact(match);
        }
        
        public void Dispose()
        {
            _readLock.Dispose();
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _helper.GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _helper.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _helper.Fetch(predicateStream);
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
        private readonly ReadHelper _helper = new(indexer._storage);
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();
        private bool _finished;
        
        public void Dispose()
        {
            if (_finished) return;
            _finished = true;
            _writeLock.Dispose();
        }
        
        public void Commit()
        {
            Dispose();
        }

        public void Rollback()
        {
            Dispose();
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _helper.CollectExact(predicateStream);
        }

        public HashSet<ImmutableDataRow>? CollectExact(int match)
        {
            return _helper.CollectExact(match);
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            return _helper.Contains(row);
        }
        
        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _helper.Fetch(predicateStream);
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

        public void Clear()
        {
            Repository.Data.Clear();
        }

        public HashSet<ImmutableDataRow>? Remove(int match)
        {
            return !Repository.Data.TryRemove(match, out var set) ? null : set;
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _helper.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly Storage _storage = new(resolver, degree);

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