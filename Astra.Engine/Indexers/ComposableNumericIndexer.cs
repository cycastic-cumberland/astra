using System.Collections;
using System.Numerics;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public static class ComposableNumericIndexer
{
    public static readonly uint[] Features = [ 
        Operation.Equal, 
        Operation.ClosedBetween,
        Operation.GreaterThan,
        Operation.GreaterOrEqualsTo,
        Operation.LesserThan,
        Operation.LesserOrEqualsTo
    ];
}

public abstract class ComposableNumericIndexer<T, TR> :
    IIndexer.IIndexerWriteHandler,
    IPointIndexer.IPointIndexerWriteHandler,
    IPointIndexer<T>.IPointIndexerWriteHandler,
    IIndexer<ComposableNumericIndexer<T, TR>, ComposableNumericIndexer<T, TR>>,
    IPointIndexer<ComposableNumericIndexer<T, TR>, ComposableNumericIndexer<T, TR>>,
    IPointIndexer<T, ComposableNumericIndexer<T, TR>, ComposableNumericIndexer<T, TR>>, 
    IRangeIndexer<T>,
    IRangeIndexer<T>.IRangeIndexerReadHandler
    where T : unmanaged, INumber<T>
    where TR : IColumnResolver<T>
{
    private readonly DataType _type;
    private readonly TR _resolver;
    private readonly BTreeMap<T, HashSet<ImmutableDataRow>> _data;

    protected ComposableNumericIndexer(TR resolver, int degree)
    {
        _type = resolver.Type;
        _resolver = resolver;
        _data = new(degree);
    }
    
    public HashSet<ImmutableDataRow>? CollectExact(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var value = predicateStream.ReadUnmanagedStruct<T>();
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

    public IEnumerable<ImmutableDataRow> ClosedBetween(T left, T right)
    {
        foreach (var (_, set) in _data.Collect(left, right, CollectionMode.ClosedInterval))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<ImmutableDataRow> ClosedBetween(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var left = predicateStream.ReadUnmanagedStruct<T>();
        var right = predicateStream.ReadUnmanagedStruct<T>();
        return ClosedBetween(left, right);
    }

    public IEnumerable<ImmutableDataRow> GreaterThan(T left)
    {
        foreach (var (_, set) in _data.CollectFrom(left, false))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<ImmutableDataRow> GreaterThan(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var left = predicateStream.ReadUnmanagedStruct<T>();
        return GreaterThan(left);
    }

    public IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(T left)
    {
        foreach (var (_, set) in _data.CollectFrom(left))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<ImmutableDataRow> GreaterOrEqualsTo(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var left = predicateStream.ReadUnmanagedStruct<T>();
        return GreaterOrEqualsTo(left);
    }

    public IEnumerable<ImmutableDataRow> LesserThan(T right)
    {
        foreach (var (_, set) in _data.CollectTo(right, false))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<ImmutableDataRow> LesserThan(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var right = predicateStream.ReadUnmanagedStruct<T>();
        return LesserThan(right);
    }

    public IEnumerable<ImmutableDataRow> LesserOrEqualsTo(T right)
    {
        foreach (var (_, set) in _data.CollectTo(right))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<ImmutableDataRow> LesserOrEqualsTo(Stream predicateStream)
    {
        predicateStream.CheckDataType(_type);
        var right = predicateStream.ReadUnmanagedStruct<T>();
        return LesserOrEqualsTo(right);
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
            Operation.ClosedBetween => ClosedBetween(predicateStream),
            Operation.GreaterThan => GreaterThan(predicateStream),
            Operation.GreaterOrEqualsTo => GreaterOrEqualsTo(predicateStream),
            Operation.LesserThan => LesserThan(predicateStream),
            Operation.LesserOrEqualsTo => LesserOrEqualsTo(predicateStream),
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
        var value = predicateStream.ReadUnmanagedStruct<T>();
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

    public ComposableNumericIndexer<T, TR> Read() => this;
    public ComposableNumericIndexer<T, TR> Write() => this;
    IPointIndexer<T>.IPointIndexerWriteHandler IPointIndexer<T>.Write() => this;
    IPointIndexer<T>.IPointIndexerReadHandler IPointIndexer<T>.Read() => this;
    IPointIndexer.IPointIndexerWriteHandler IPointIndexer.Write() => this;
    IPointIndexer.IPointIndexerReadHandler IPointIndexer.Read() => this;
    IIndexer.IIndexerWriteHandler IIndexer.Write() => this;
    IIndexer.IIndexerReadHandler IIndexer.Read() => this;
    IRangeIndexer<T>.IRangeIndexerReadHandler IRangeIndexer<T>.Read() => this;
    
    public FeaturesList SupportedReadOperations => ComposableNumericIndexer.Features;
    public FeaturesList SupportedWriteOperations => ComposableNumericIndexer.Features;
    public uint Priority => 2;
    public DataType Type => _type;
}

public sealed class ComposableIntegerRangeIndexer(UnmanagedColumnResolver<int> resolver, int degree)
    : ComposableNumericIndexer<int, UnmanagedColumnResolver<int>>(resolver, degree);

public sealed class ComposableLongRangeIndexer(UnmanagedColumnResolver<long> resolver, int degree)
    : ComposableNumericIndexer<long, UnmanagedColumnResolver<long>>(resolver, degree);

public sealed class ComposableSingleRangeIndexer(UnmanagedColumnResolver<float> resolver, int degree)
    : ComposableNumericIndexer<float, UnmanagedColumnResolver<float>>(resolver, degree);

public sealed class ComposableDoubleRangeIndexer(UnmanagedColumnResolver<double> resolver, int degree)
    : ComposableNumericIndexer<double, UnmanagedColumnResolver<double>>(resolver, degree);

public sealed class ComposableDecimalRangeIndexer(UnmanagedColumnResolver<decimal> resolver, int degree)
    : ComposableNumericIndexer<decimal, UnmanagedColumnResolver<decimal>>(resolver, degree);

