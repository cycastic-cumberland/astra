using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Collections;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.v2.Indexers;
using Astra.TypeErasure.Planners;
using Microsoft.IO;

namespace Astra.Engine.v2.Data;

public static class Aggregator
{
    private static readonly ThreadLocal<HashSet<DataRow>?> LocalSet = new();
    private static IEnumerable<DataRow> IntersectInternal(IEnumerable<DataRow> lhs,
        IEnumerable<DataRow> rhs)
    {
        var set = LocalSet.Value ?? new();
        LocalSet.Value = null;
        try
        {
            foreach (var row in lhs)
            {
                set.Add(row);
            }

            foreach (var row in rhs)
            {
                if (set.Contains(row))
                    yield return row;
            }
        }
        finally
        {
            if (LocalSet.Value == null)
            {
                set.Clear();
                LocalSet.Value = set;
            }
        }
    }
    private static IEnumerable<DataRow> IntersectInternal(HashSet<DataRow> set,
        IEnumerable<DataRow> rhs)
    {
        return rhs.Where(set.Contains);
    }
    private static IEnumerable<DataRow> Intersect(IEnumerable<DataRow> lhs, IEnumerable<DataRow> rhs)
    {
        if (lhs is HashSet<DataRow> lSet)
            return IntersectInternal(lSet, rhs);
        if (rhs is HashSet<DataRow> rSet)
            return IntersectInternal(rSet, lhs);
        return IntersectInternal(lhs, rhs);
    }

    private static IEnumerable<DataRow> UnionInternal(IEnumerable<DataRow> lhs, IEnumerable<DataRow> rhs)
    {
        var set = LocalSet.Value ?? new();
        LocalSet.Value = null;
        try
        {
            foreach (var row in lhs)
            {
                set.Add(row);
                yield return row;
            }

            foreach (var row in rhs)
            {
                if (set.Add(row))
                    yield return row;
            }
        }
        finally
        {
            if (LocalSet.Value == null)
            {
                set.Clear();
                LocalSet.Value = set;
            }
        }
    }
    
    private static IEnumerable<DataRow> UnionInternal(HashSet<DataRow> set,
        IEnumerable<DataRow> rhs)
    {
        foreach (var row in set)
        {
            yield return row;
        }

        foreach (var row in rhs)
        {
            if (!set.Contains(row))
                yield return row;
        }
    }

    private static IEnumerable<DataRow>? IntersectSelect(IEnumerable<DataRow>? left, IEnumerable<DataRow>? right)
    {
        return left switch
        {
            null when right == null => null,
            null => right,
            not null when right == null => left,
            _ => Intersect(left, right)
        };
    }
    
    private static IEnumerable<DataRow>? RecursiveIntersect<T>(Stream predicateStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    {
        var left = Aggregate(predicateStream, in readers);
        var right = Aggregate(predicateStream, in readers);
        return IntersectSelect(left, right);
    }
    
    private static IEnumerable<DataRow> Union(IEnumerable<DataRow> lhs,
        IEnumerable<DataRow> rhs)
    {
        if (lhs is HashSet<DataRow> lSet)
            return UnionInternal(lSet, rhs);
        if (rhs is HashSet<DataRow> rSet)
            return UnionInternal(rSet, lhs);
        return UnionInternal(lhs, rhs);
    }
    
    private static IEnumerable<DataRow>? UnionSelect(IEnumerable<DataRow>? left, IEnumerable<DataRow>? right)
    {
        return left switch
        {
            null when right == null => null,
            null => right,
            not null when right == null => left,
            _ => Union(left, right)
        };
    }
    
    private static IEnumerable<DataRow>? RecursiveUnion<T>(Stream predicateStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    {
        var left = Aggregate(predicateStream, in readers);
        var right = Aggregate(predicateStream, in readers);
        return UnionSelect(left, right);
    }
    private static IEnumerable<DataRow>? Filter<T>(Stream predicateStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    {
        var offset = predicateStream.ReadInt();
        ref readonly var reader = ref readers[offset];
        return reader?.Fetch(predicateStream);
    }
    
    private struct AggregatorStackFrame
    {
        public uint Type;
        public bool LhsSet;
        public bool RhsSet;
        public IEnumerable<DataRow>? Lhs;
        public IEnumerable<DataRow>? Rhs;
    }

    private static void SetFrame(ref ArrayStack<AggregatorStackFrame> stack, ref AggregatorStackFrame frame,
        IEnumerable<DataRow>? target)
    {
        if (frame.LhsSet == false)
        {
            stack.Add(frame with {Lhs = target, LhsSet = true});
            return;
        }

        stack.Add(frame with {Rhs = target, RhsSet = true});
    }

    public static IEnumerable<DataRow>? Aggregate<T>(Stream predicateStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    { 
        var stack = new ArrayStack<AggregatorStackFrame>(32); // 32 should be plenty enough tbh...
        try
        {
            var type = predicateStream.ReadUInt();
            stack.Push(new AggregatorStackFrame
            {
                Type = type,
                LhsSet = false,
                RhsSet = false,
                Lhs = null,
                Rhs = null,
            });
            while (stack.TryPop(out var frame))
            {
                type = frame.Type;
                switch (type)
                {
                    case QueryType.IntersectMask:
                    {
                        if (frame is { LhsSet: true, RhsSet: true })
                        {
                            var intersected = IntersectSelect(frame.Lhs, frame.Rhs);
                            if (!stack.TryPop(out var prev)) return intersected;
                            SetFrame(ref stack, ref prev, intersected);
                        }

                        goto case QueryType.FilterMask + 1;
                    }
                    case QueryType.UnionMask:
                    {
                        if (frame is { LhsSet: true, RhsSet: true })
                        {
                            var intersected = UnionSelect(frame.Lhs, frame.Rhs);
                            if (!stack.TryPop(out var prev)) return intersected;
                            SetFrame(ref stack, ref prev, intersected);
                        }

                        goto case QueryType.FilterMask + 1;
                    }
                    case QueryType.FilterMask:
                    {
                        var filtered = Filter(predicateStream, in readers);
                        if (!stack.TryPop(out var prev)) return filtered;
                        SetFrame(ref stack, ref prev, filtered);
                        continue;
                    }
                    case QueryType.FilterMask + 1:
                    {
                        stack.Push(frame);
                        type = predicateStream.ReadUInt();
                        stack.Push(new AggregatorStackFrame
                        {
                            Type = type,
                            LhsSet = false,
                            RhsSet = false,
                            Lhs = null,
                            Rhs = null,
                        });
                        break;
                    }
                    default:
                        throw new AggregateException($"Aggregator type not supported: {type}");
                }
            }

            return null;
        }
        finally
        {
            stack.Dispose();
        }
    }

    public static IEnumerable<DataRow>? ApplyPhysicalPlan<T>(ref readonly PhysicalPlan plan, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    {
        var lSet = false;
        IEnumerable<DataRow>? lhs = null;
        IEnumerable<DataRow>? rhs = null;
        var blueprints = plan.Blueprints;
        for (var i = blueprints.Length - 1; i >= 0; i--)
        {
            ref readonly var blueprint = ref blueprints[i];
            switch (blueprint.QueryOperationType)
            {
                case QueryType.IntersectMask:
                {
                    var results = IntersectSelect(lhs, rhs);
                    lhs = results;
                    rhs = null;
                    lSet = true;
                    break;
                }
                case QueryType.UnionMask:
                {
                    var results = UnionSelect(lhs, rhs);
                    lhs = results;
                    rhs = null;
                    lSet = true;
                    break;
                }
                case QueryType.FilterMask:
                {
                    var results = blueprint.Offset switch
                    {
                        < 0 => null,
                        >= 0 => readers[blueprint.Offset]?.Fetch(in blueprint)
                    };
                    if (lSet)
                    {
                        rhs = results;
                    }
                    else
                    {
                        lhs = results;
                        lSet = true;
                    }
                    break;
                }
                default:
                    throw new AggregateException($"Aggregator type not supported: {blueprint.QueryOperationType}");
            }
        }

        return lhs;
    }
}

public struct PreparedLocalEnumerator<T> : IEnumerator<T>
    where T : IAstraSerializable
{
    private readonly HashSet<DataRow>? _result;
    private IEnumerator<DataRow> _enumerator = null!;
    private T _current = default!;
    private RecyclableMemoryStream _buffer = null!;
    private int _stage;

    public PreparedLocalEnumerator(HashSet<DataRow>? result)
    {
        _result = result;
        _stage = 1;
    }

    public void Dispose()
    {
        _enumerator?.Dispose();
        _buffer?.Dispose();
        _buffer = null!;
    }

    public bool MoveNext()
    {
        switch (_stage)
        {
            case 1:
            {
                if (_result == null) return false;
                _buffer = MemoryStreamPool.Allocate();
                _enumerator = _result.GetEnumerator();
                _stage = 2;
                goto case 2;
            }
            case 2:
            {
                if (!_enumerator.MoveNext()) goto case 3;
                _buffer.Position = 0;
                var row = _enumerator.Current;
                row.Serialize(_buffer);
                _buffer.Position = 0;
                var value = Activator.CreateInstance<T>();
                value.DeserializeStream(new ForwardStreamWrapper(_buffer));
                _current = value;
                return true;
            }
            case 3:
            {
                _enumerator.Dispose();
                _buffer.Dispose();
                _enumerator = null!;
                _buffer = null!;
                goto case -1;
            }
            case -1:
            {
                _stage = 0;
                goto default;
            }
            default:
                return false;
        }
    }

    public void Reset()
    {
        Dispose();
        _stage = 1;
    }

    public T Current => _current;

    object IEnumerator.Current => Current!;
}

public readonly struct PreparedLocalEnumerable<T> : IEnumerable<T> 
    where T : IAstraSerializable
{
    private readonly HashSet<DataRow>? _result;
    
    public PreparedLocalEnumerable(HashSet<DataRow>? result) => _result = result;
    public PreparedLocalEnumerator<T> GetEnumerator() => new(_result);
    
    IEnumerator<T> IEnumerable<T> .GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class AggregatorHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<DataRow>? Aggregate<T>(this Stream predicateStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
        => Aggregator.Aggregate(predicateStream, in readers);

    public static void Aggregate<T>(this Stream predicateStream, Stream outStream, ref readonly Span<T?> readers)
        where T : struct, BaseIndexer.IReadable
    {
        var result = predicateStream.Aggregate(in readers);
        if (result == null)
        {
            outStream.WriteValue(CommonProtocol.EndOfSetFlag);
            return;
        }
        var flag = CommonProtocol.HasRow;
        foreach (var row in result)
        {
            outStream.WriteValue(flag);
            flag = CommonProtocol.ChainedFlag;
            row.Serialize(outStream);
        }
        outStream.WriteValue(CommonProtocol.EndOfSetFlag);
    }

    public static PreparedLocalEnumerable<T> LocalAggregate<T, TReader>(this Stream predicateStream, ref readonly Span<TReader?> readers)
        where T : IAstraSerializable
        where TReader : struct, BaseIndexer.IReadable
    {
        var set = predicateStream.Aggregate(in readers)?.ToHashSet();
        return new(set);
    }
    
    public static PreparedLocalEnumerable<T> LocalAggregate<T, TReader>(this ref readonly PhysicalPlan plan, ref readonly Span<TReader?> readers)
        where T : IAstraSerializable
        where TReader : struct, BaseIndexer.IReadable
    {
        var set = Aggregator.ApplyPhysicalPlan(in plan, in readers)?.ToHashSet();
        return new(set);
    }
}