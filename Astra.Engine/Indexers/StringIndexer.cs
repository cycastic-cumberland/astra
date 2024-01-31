using System.Collections;
using Astra.Common;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class StringIndexer(StringColumnResolver resolver) :
    IIndexer<StringIndexer.ReadHandler, StringIndexer.WriteHandler>,
    IPointIndexer<StringIndexer.ReadHandler, StringIndexer.WriteHandler>,
    IPointIndexer<string, StringIndexer.ReadHandler, StringIndexer.WriteHandler>
{
    private class Storage(StringColumnResolver resolver)
    {
        public StringColumnResolver Resolver => resolver;
        public Dictionary<string, HashSet<ImmutableDataRow>> Data { get; } = new();
        public RWLock Lock { get; } = RWLock.Create();
    }

    private readonly struct InternalReadHandler(Storage repository) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<string, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
        {
            predicateStream.CheckDataType(DataType.String);
            var value = predicateStream.ReadString();
            return CollectExact(value);
        }

        public HashSet<ImmutableDataRow>? CollectExact(string match)
        {
            repository.Data.TryGetValue(match, out var set);
            return set;
        }
        
        public void Dispose()
        {
            
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
            var index = repository.Resolver.Dump(row);
            return repository.Data.TryGetValue(index, out var set) && set.Contains(row);
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
    }

    public readonly struct ReadHandler(StringIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerReadHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerReadHandler,
        IPointIndexer<string, ReadHandler, WriteHandler>.IPointIndexerReadHandler
    {
        private readonly RWLock.ReadLockInstance _readLock = indexer._storage.Lock.Read();
        private readonly InternalReadHandler _handler = new(indexer._storage);

        public HashSet<ImmutableDataRow>? CollectExact(string match)
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

    public struct WriteHandler(StringIndexer indexer) :
        IIndexer<ReadHandler, WriteHandler>.IIndexerWriteHandler,
        IPointIndexer<ReadHandler, WriteHandler>.IPointIndexerWriteHandler,
        IPointIndexer<string, ReadHandler, WriteHandler>.IPointIndexerWriteHandler
    {
        private Storage Repository => indexer._storage;
        private readonly RWLock.WriteLockInstance _writeLock = indexer._storage.Lock.Write();
        private readonly InternalReadHandler _handler = new(indexer._storage);
        private bool _finished;

        public HashSet<ImmutableDataRow>? CollectExact(string match)
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
            predicateStream.CheckDataType(DataType.String);
            var value = predicateStream.ReadString();
            return Remove(value);
        }

        public bool RemoveExact(ImmutableDataRow row)
        {
            var index = Repository.Resolver.Dump(row);
            return Repository.Data.TryGetValue(index, out var set) && set.Remove(row);
        }

        public void Clear()
        {
            Repository.Resolver.Clear();
            Repository.Data.Clear();
        }

        public HashSet<ImmutableDataRow>? Remove(string match)
        {
            return !Repository.Data.Remove(match, out var set) ? null : set;
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
    IPointIndexer<string>.IPointIndexerWriteHandler IPointIndexer<string>.Write()
    {
        return Write();
    }

    IPointIndexer<string>.IPointIndexerReadHandler IPointIndexer<string>.Read()
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