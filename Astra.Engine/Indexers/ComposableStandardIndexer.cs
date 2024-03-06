using System.Collections;
using Astra.Common;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public static class StandardComposableIndexer
{
    public static readonly uint[] Features = [ Operation.Equal ];
}

public abstract class ComposableStandardIndexer<T, TColumnResolver, TStreamResolver> :
    IIndexer.IIndexerWriteHandler,
    IIndexer<ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>, ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>>,
    IPointIndexer.IPointIndexerWriteHandler,
    IPointIndexer<T>.IPointIndexerWriteHandler,
    IPointIndexer<ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>, ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>>,
    IPointIndexer<T, ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>, ComposableStandardIndexer<T, TColumnResolver, TStreamResolver>>
    where TColumnResolver : IColumnResolver<T>
    where TStreamResolver : IStreamResolver<T>
    where T : notnull
{
    private readonly DataType _type;
    private readonly TColumnResolver _resolver;
    private readonly Dictionary<T, HashSet<ImmutableDataRow>> _data = new();

    protected ComposableStandardIndexer(TColumnResolver columnResolver)
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
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }
        
    public void Dispose()
    {
            
    }
    
    public void Commit()
    {
            
    }

    public void Rollback()
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
        return !_data.Remove(match, out var set) ? null : set;
    }

    public ComposableStandardIndexer<T, TColumnResolver, TStreamResolver> Read() => this;
    public ComposableStandardIndexer<T, TColumnResolver, TStreamResolver> Write() => this;
    IPointIndexer<T>.IPointIndexerWriteHandler IPointIndexer<T>.Write() => this;
    IPointIndexer<T>.IPointIndexerReadHandler IPointIndexer<T>.Read() => this;
    IPointIndexer.IPointIndexerWriteHandler IPointIndexer.Write() => this;
    IPointIndexer.IPointIndexerReadHandler IPointIndexer.Read() => this;
    IIndexer.IIndexerWriteHandler IIndexer.Write() => this;
    IIndexer.IIndexerReadHandler IIndexer.Read() => this;
    
    public FeaturesList SupportedReadOperations => StandardComposableIndexer.Features;
    public FeaturesList SupportedWriteOperations => StandardComposableIndexer.Features;
    public uint Priority => 0;
    public DataType Type => _type;
}

public sealed class ComposableIntegerIndexer(IntegerColumnResolver resolver) :
    ComposableStandardIndexer<int, IntegerColumnResolver, IntegerStreamResolver>(resolver);

public sealed class ComposableLongIndexer(LongColumnResolver resolver) :
    ComposableStandardIndexer<long, LongColumnResolver, LongStreamResolver>(resolver);

public sealed class ComposableSingleIndexer(SingleColumnResolver resolver) :
    ComposableStandardIndexer<float, SingleColumnResolver, SingleStreamResolver>(resolver);

public sealed class ComposableDoubleIndexer(DoubleColumnResolver resolver) :
    ComposableStandardIndexer<double, DoubleColumnResolver, DoubleStreamResolver>(resolver);

public sealed class ComposableBytesIndexer(BytesColumnResolver resolver) :
    ComposableStandardIndexer<ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>(resolver);

public sealed class ComposableStringIndexer(StringColumnResolver resolver) :
    ComposableStandardIndexer<StringWrapper, StringColumnResolver, StringWrapperStreamResolver>(resolver);

public sealed class ComposableDecimalIndexer(DecimalColumnResolver resolver) :
    ComposableStandardIndexer<decimal, DecimalColumnResolver, DecimalStreamResolver>(resolver);
