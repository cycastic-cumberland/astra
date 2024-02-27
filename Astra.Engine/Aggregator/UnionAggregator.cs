using Astra.Engine.Data;

namespace Astra.Engine.Aggregator;

public readonly struct UnionAggregator : IAggregatorStream
{
    private static readonly ThreadLocal<HashSet<ImmutableDataRow>?> LocalSet = new();
    private static IEnumerable<ImmutableDataRow> UnionInternal(IEnumerable<ImmutableDataRow> lhs, IEnumerable<ImmutableDataRow> rhs)
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

    private static IEnumerable<ImmutableDataRow> UnionInternal(HashSet<ImmutableDataRow> set,
        IEnumerable<ImmutableDataRow> rhs)
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

    private static IEnumerable<ImmutableDataRow> Union(IEnumerable<ImmutableDataRow> lhs,
        IEnumerable<ImmutableDataRow> rhs)
    {
        if (lhs is HashSet<ImmutableDataRow> lSet)
            return UnionInternal(lSet, rhs);
        if (rhs is HashSet<ImmutableDataRow> rSet)
            return UnionInternal(rSet, lhs);
        return UnionInternal(lhs, rhs);
    }
    
    public IEnumerable<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock) where T : struct, DataRegistry.IIndexersLock
    {
        var left = IAggregatorStream.Aggregate(predicateStream, indexersLock);
        var right = IAggregatorStream.Aggregate(predicateStream, indexersLock);
        return left switch
        {
            null when right == null => null,
            null => right,
            not null when right == null => left,
            _ => Union(left, right)
        };
    }
}