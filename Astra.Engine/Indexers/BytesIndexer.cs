using System.Collections;
using Astra.Common;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

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

    private readonly struct InternalReadHandler(Storage repository) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<BytesCluster, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.Bytes);
            using var value = predicateStream.ReadCluster();
            return CollectExact(value);
        }
        public HashSet<ImmutableDataRow>? CollectExact(BytesCluster match)
        {
            var hash = Hash128.HashMd5(match.Reader);
            repository.Data.TryGetValue(hash, out var set);
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public bool Contains(ImmutableDataRow row)
        {
            var (_, hash) = repository.Resolver.Dump(row);
            return repository.Data.TryGetValue(hash, out var set) && set.Contains(row);
        }
        
        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
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
            
        }
    }

    public readonly struct ReadHandler(BytesIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<BytesCluster, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();
        private readonly InternalReadHandler _handler = new(indexer._storage);

        public HashSet<ImmutableDataRow>? CollectExact(BytesCluster match)
        {
            return _handler.CollectExact(match);
        }

        public void Dispose()
        {
            _readLock.Dispose();
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _handler.CollectExact(predicateStream);
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _handler.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_handler).GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _handler.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _handler.Fetch(predicateStream);
        }
    }

    public struct WriteHandler(BytesIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerWriteHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IPointIndexer<BytesCluster, ReadHandler, WriteHandler>.IPointIndexerWriteHandler
    {
        private Storage Repository => indexer._storage;
        private readonly InternalReadHandler _handler = new(indexer._storage);
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();
        private bool _finished;

        public HashSet<ImmutableDataRow>? CollectExact(BytesCluster match)
        {
            return _handler.CollectExact(match);
        }

        public void Dispose()
        {
            if (_finished) return;
            _finished = true;
            _writeLock.Dispose();
        }

        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            return _handler.CollectExact(predicateStream);
        }

        public void Commit()
        {
            Dispose();
        }

        public void Rollback()
        {
            Dispose();
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

        public void Clear()
        {
            Repository.Resolver.Clear();
            Repository.Data.Clear();
        }

        public HashSet<ImmutableDataRow>? Remove(BytesCluster match)
        {
            var hash = Hash128.HashMd5(match.Reader);
            return !Repository.Data.Remove(hash, out var set) ? null : set;
        }

        public IEnumerator<ImmutableDataRow> GetEnumerator()
        {
            return _handler.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_handler).GetEnumerator();
        }

        public bool Contains(ImmutableDataRow row)
        {
            return _handler.Contains(row);
        }

        public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
        {
            return _handler.Fetch(predicateStream);
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