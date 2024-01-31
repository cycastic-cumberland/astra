using System.Collections;
using System.Numerics;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public abstract class NumericIndexer<T, TR>(TR resolver, int degree) :
        IIndexer<NumericIndexer<T, TR>.ReadHandler, NumericIndexer<T, TR>.WriteHandler>,
        IPointIndexer<NumericIndexer<T, TR>.ReadHandler, NumericIndexer<T, TR>.WriteHandler>,
        IPointIndexer<T, NumericIndexer<T, TR>.ReadHandler, NumericIndexer<T, TR>.WriteHandler>, 
        IRangeIndexer<T>
    where T : unmanaged, INumber<T>
    where TR : IColumnResolver<T>
{
    private class Storage(TR resolver, int degree)
    {
        public readonly TR Resolver = resolver;
        public readonly BTreeMap<T, HashSet<ImmutableDataRow>> Data = new(degree);
        public readonly RWLock Lock = RWLock.Create();
        public readonly DataType TypeMask = resolver.Type;
    }

    private readonly struct InternalReadHandler(Storage repository) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<T, ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IRangeIndexer<T>.IRangeIndexerReadHandler
    {
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var value = predicateStream.ReadUnmanagedStruct<T>();
            return CollectExact(value);
        }

        public HashSet<ImmutableDataRow>? CollectExact(T match)
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

        public IEnumerable<ImmutableDataRow> ClosedBetween(T left, T right)
        {
            foreach (var (_, set) in repository.Data.Collect(left, right, CollectionMode.ClosedInterval))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> ClosedBetween(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var left = predicateStream.ReadUnmanagedStruct<T>();
            var right = predicateStream.ReadUnmanagedStruct<T>();
            return ClosedBetween(left, right);
        }

        public IEnumerable<ImmutableDataRow> GreaterThan(T left)
        {
            foreach (var (_, set) in repository.Data.CollectFrom(left, false))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> GreaterThan(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var left = predicateStream.ReadUnmanagedStruct<T>();
            return GreaterThan(left);
        }

        public IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(T left)
        {
            foreach (var (_, set) in repository.Data.CollectFrom(left))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var left = predicateStream.ReadUnmanagedStruct<T>();
            return GreaterOrEqualsTo(left);
        }

        public IEnumerable<ImmutableDataRow> LesserThan(T right)
        {
            foreach (var (_, set) in repository.Data.CollectTo(right, false))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> LesserThan(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var right = predicateStream.ReadUnmanagedStruct<T>();
            return LesserThan(right);
        }

        public IEnumerable<ImmutableDataRow> LesserOrEqualsTo(T right)
        {
            foreach (var (_, set) in repository.Data.CollectTo(right))
            {
                foreach (var row in set)
                {
                    yield return row;
                }
            }
        }
        
        private IEnumerable<ImmutableDataRow> LesserOrEqualsTo(Stream predicateStream)
        {
            predicateStream.CheckDataType(repository.TypeMask);
            var right = predicateStream.ReadUnmanagedStruct<T>();
            return LesserOrEqualsTo(right);
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

    public readonly struct ReadHandler(NumericIndexer<T, TR> indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<T, ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IRangeIndexer<T>.IRangeIndexerReadHandler
    {
        private readonly InternalReadHandler _handler = new(indexer._storage);
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();
        
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _handler.CollectExact(predicateStream);
        }

        public HashSet<ImmutableDataRow>? CollectExact(T match)
        {
            return _handler.CollectExact(match);
        }
        
        public void Dispose()
        {
            _readLock.Dispose();
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _handler.GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _handler.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _handler.Fetch(predicateStream);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<ImmutableDataRow> ClosedBetween(T left, T right)
        {
            return _handler.ClosedBetween(left, right);
        }

        public IEnumerable<ImmutableDataRow> GreaterThan(T left)
        {
            return _handler.GreaterThan(left);
        }

        public IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(T left)
        {
            return _handler.GreaterOrEqualsTo(left);
        }

        public IEnumerable<ImmutableDataRow> LesserThan(T right)
        {
            return _handler.LesserThan(right);
        }

        public IEnumerable<ImmutableDataRow> LesserOrEqualsTo(T right)
        {
            return _handler.LesserOrEqualsTo(right);
        }
    }

    public struct WriteHandler(NumericIndexer<T, TR> indexer) : 
        IIndexer<ReadHandler, WriteHandler>.IIndexerWriteHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IPointIndexer<T, ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IRangeIndexer<T>.IRangeIndexerReadHandler
    {
        private Storage Repository => indexer._storage;
        private readonly InternalReadHandler _handler = new(indexer._storage);
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
            return _handler.CollectExact(predicateStream);
        }

        public HashSet<ImmutableDataRow>? CollectExact(T match)
        {
            return _handler.CollectExact(match);
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            return _handler.Contains(row);
        }
        
        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _handler.Fetch(predicateStream);
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
            predicateStream.CheckDataType(Repository.TypeMask);
            var value = predicateStream.ReadUnmanagedStruct<T>();
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

        public HashSet<ImmutableDataRow>? Remove(T match)
        {
            return !Repository.Data.TryRemove(match, out var set) ? null : set;
        }
        
        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _handler.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<ImmutableDataRow> ClosedBetween(T left, T right)
        {
            return _handler.ClosedBetween(left, right);
        }

        public IEnumerable<ImmutableDataRow> GreaterThan(T left)
        {
            return _handler.GreaterThan(left);
        }

        public IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(T left)
        {
            return _handler.GreaterOrEqualsTo(left);
        }

        public IEnumerable<ImmutableDataRow> LesserThan(T right)
        {
            return _handler.LesserThan(right);
        }

        public IEnumerable<ImmutableDataRow> LesserOrEqualsTo(T right)
        {
            return _handler.LesserOrEqualsTo(right);
        }
    }

    private readonly Storage _storage = new(resolver, degree);

    public ReadHandler Read() => new(this);
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

    IIndexer.IIndexerReadHandler IIndexer.Read()
    {
        return Read();
    }

    public WriteHandler Write() => new(this);
    
    IRangeIndexer<T>.IRangeIndexerReadHandler IRangeIndexer<T>.Read()
    {
        return Read();
    }
}

public sealed class IntegerIndexer(IntegerColumnResolver resolver, int degree)
    : NumericIndexer<int, IntegerColumnResolver>(resolver, degree);

public sealed class LongIndexer(LongColumnResolver resolver, int degree)
    : NumericIndexer<long, LongColumnResolver>(resolver, degree);

public sealed class SingleIndexer(SingleColumnResolver resolver, int degree)
    : NumericIndexer<float, SingleColumnResolver>(resolver, degree);

public sealed class DoubleIndexer(DoubleColumnResolver resolver, int degree)
    : NumericIndexer<double, DoubleColumnResolver>(resolver, degree);
