using System.Collections;
using Astra.Collections.Trigram;
using Astra.Common;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

file static class FuzzyIndexer
{
    public static readonly uint[] Features = [ Operation.Equal, Operation.FuzzySearch ];
}

public abstract class ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>  :
    IIndexer.IIndexerWriteHandler,
    IPointIndexer.IPointIndexerWriteHandler,
    IIndexer<ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>, ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>>,
    IPointIndexer<ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>, ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>>,
    IPointIndexer<T>.IPointIndexerWriteHandler,
    IPointIndexer<T, ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>, ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>>,
    IFuzzyIndexer<TUnit, T, ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver>>, 
    IFuzzyIndexer<TUnit, T>.IFuzzyIndexerHandler 
    where TUnit : IEquatable<TUnit>
    where T : IReadOnlyList<TUnit>
    where TColumnResolver : IColumnResolver<T>
    where TStreamResolver : IStreamResolver<T>
{
    public struct FuzzySearchEnumerator(
        Dictionary<T, (HashSet<ImmutableDataRow> set, int matchedLength)> matches) :
        IEnumerator<(ImmutableDataRow row, int matchedLength)>
    {
        private Dictionary<T, (HashSet<ImmutableDataRow> row, int matchedLength)>.Enumerator _dictEnumerator =
            matches.GetEnumerator();
        private HashSet<ImmutableDataRow>.Enumerator? _setEnumerator;
        private int _stage = -1;
        private int _matchLength;
        private (ImmutableDataRow set, int matchedLength) _current = new();

        public void Dispose()
        {
            _dictEnumerator.Dispose();
            _setEnumerator?.Dispose();
        }

        public bool MoveNext()
        {
            while (true)
            {
                switch (_stage)
                {
                    case -1:
                    {
                        if (!_dictEnumerator.MoveNext())
                        {
                            _stage = 0;
                            continue;
                        }

                        (_, (var set, _matchLength)) = _dictEnumerator.Current;

                        // ReSharper disable once NotDisposedResource
                        _setEnumerator = set.GetEnumerator();
                        _stage = 1;
                        continue;
                    }
                    case 1:
                    {
                        var enumerator = _setEnumerator!.Value;
                        if (!enumerator.MoveNext())
                        {
                            _stage = -1;
                            enumerator.Dispose();
                            _setEnumerator = null;
                            continue;
                        }

                        _current = (enumerator.Current, _matchLength);
                        return true;
                    }
                    default:
                        return false;
                }
            }
        }

        public void Reset()
        {
            _stage = -1;
            _current = new();
            _dictEnumerator.Dispose();
            _dictEnumerator = matches.GetEnumerator();
            _setEnumerator?.Dispose();
            _setEnumerator = null;
        }

        public (ImmutableDataRow row, int matchedLength) Current => _current;

        object IEnumerator.Current => Current;
    }

    public readonly struct FuzzySearchEnumerable(
        Dictionary<T, (HashSet<ImmutableDataRow> set, int matchedLength)> matches) :
        IEnumerable<(ImmutableDataRow row, int matchedLength)>
    {
        public FuzzySearchEnumerator GetEnumerator()
        {
            return new(matches);
        }
        
        IEnumerator<(ImmutableDataRow row, int matchedLength)> IEnumerable<(ImmutableDataRow row, int matchedLength)>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private struct ReducedFuzzySearchEnumerator(FuzzySearchEnumerable matches) : IEnumerator<ImmutableDataRow>
    {
        private FuzzySearchEnumerator _enumerator = matches.GetEnumerator();
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public ImmutableDataRow Current => _enumerator.Current.row;

        object IEnumerator.Current => Current;
    }

    private readonly struct ReducedFuzzySearchEnumerable(
        FuzzySearchEnumerable matches) :
        IEnumerable<ImmutableDataRow>
    {
        public ReducedFuzzySearchEnumerator GetEnumerator()
        {
            return new(matches);
        }
        
        IEnumerator<ImmutableDataRow> IEnumerable<ImmutableDataRow>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    private readonly DataType _type;
    private readonly TColumnResolver _resolver;
    private readonly TrigramMap<TUnit, T, HashSet<ImmutableDataRow>> _data = new();
    
    protected ComposableFuzzyIndexer(TColumnResolver columnResolver)
    {
        _resolver = columnResolver;
        _type = columnResolver.Type;
    }
    
    public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var value = TStreamResolver.ConsumeStream(predicateStream);
        return CollectExact(value);
    }
    
    public HashSet<ImmutableDataRow>? CollectExact(T match)
    {
        _data.TryGetValue(match, out var set);
        return set;
    }
    
    public IEnumerator<ImmutableDataRow> GetEnumerator()
    {
        foreach (var (_, set) in _data)
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    public bool Contains(ImmutableDataRow row)
    {
        var index = _resolver.Dump(row);
        return _data.TryGetValue(index, out var set) && set.Contains(row);
    }

    public FuzzySearchEnumerable FuzzySearch(T key, int minLength)
    {
        return new(_data.FuzzySearch(key, minLength));
    }

    IEnumerable<(ImmutableDataRow row, int matchLength)> IFuzzyIndexer<TUnit, T>.IFuzzyIndexerHandler.FuzzySearch(
        T key, int minLength)
    {
        return FuzzySearch(key, minLength);
    }

    private ReducedFuzzySearchEnumerable ReducedFuzzySearch(T key, int minLength)
    {
        return new(FuzzySearch(key, minLength));
    }

    IEnumerable<ImmutableDataRow> IFuzzyIndexer<TUnit, T>.IFuzzyIndexerHandler.ReducedFuzzySearch(T key, int minLength)
    {
        return ReducedFuzzySearch(key, minLength);
    }

    private ReducedFuzzySearchEnumerable ReducedFuzzySearch(Stream predicateStream)
    {
        var key = TStreamResolver.ConsumeStream(predicateStream);
        var minLength = predicateStream.ReadInt();
        return ReducedFuzzySearch(key, minLength);
    }

    public IEnumerable<ImmutableDataRow>? Fetch(Stream predicateStream)
    {
        var op = predicateStream.ReadUInt();
        return Fetch(op, predicateStream);
    }
    
    public IEnumerable<ImmutableDataRow>? Fetch(uint operation, Stream predicateStream)
    {
        return operation switch
        {
            Operation.Equal => CollectExact(predicateStream),
            Operation.FuzzySearch => ReducedFuzzySearch(predicateStream),
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }
    
    public void Dispose()
    {
        
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public void Add(ImmutableDataRow row)
    {
        var index = _resolver.Dump(row);
        if (!_data.TryGetValue(index, out var set))
        {
            set = new();
            _data[index] = set;
        }

        set.Add(row);
    }

    public HashSet<ImmutableDataRow>? Remove(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var value = TStreamResolver.ConsumeStream(predicateStream);
        return Remove(value);
    }
        
    public bool RemoveExact(ImmutableDataRow row)
    {
        var index = _resolver.Dump(row);
        return _data.TryGetValue(index, out var set) && set.Remove(row);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public HashSet<ImmutableDataRow>? Remove(T match)
    {
        return !_data.TryRemove(match, out var set) ? null : set;
    }

    public void Commit()
    {
        
    }

    public void Rollback()
    {
        
    }
    
    public ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver> Read() => this;
    public ComposableFuzzyIndexer<TUnit, T, TColumnResolver, TStreamResolver> Write() => this;
    IPointIndexer<T>.IPointIndexerWriteHandler IPointIndexer<T>.Write() => this;
    IPointIndexer<T>.IPointIndexerReadHandler IPointIndexer<T>.Read() => this;
    IPointIndexer.IPointIndexerWriteHandler IPointIndexer.Write() => this;
    IPointIndexer.IPointIndexerReadHandler IPointIndexer.Read() => this;
    IIndexer.IIndexerWriteHandler IIndexer.Write() => this;
    IIndexer.IIndexerReadHandler IIndexer.Read() => this;

    public FeaturesList SupportedReadOperations => FuzzyIndexer.Features;
    public FeaturesList SupportedWriteOperations => FuzzyIndexer.Features;
    public uint Priority => 2;
    public DataType Type => _type;
}

public sealed class ComposableFuzzyStringIndexer(StringColumnResolver resolver)
    : ComposableFuzzyIndexer<char, StringWrapper, StringColumnResolver, StringWrapperStreamResolver>(resolver);

public sealed class ComposableFuzzyBytesIndexer(BytesColumnResolver resolver)
    : ComposableFuzzyIndexer<byte, ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>(resolver);
