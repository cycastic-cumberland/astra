using Astra.Engine.Data;

namespace Astra.Engine.Aggregator;

public readonly struct IntersectAggregator : IAggregatorStream
{
    private static readonly ThreadLocal<HashSet<ImmutableDataRow>?> LocalSet = new();

    private static IEnumerable<ImmutableDataRow> IntersectInternal(IEnumerable<ImmutableDataRow> lhs,
        IEnumerable<ImmutableDataRow> rhs)
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
    
    private static IEnumerable<ImmutableDataRow> IntersectInternal(HashSet<ImmutableDataRow> set,
        IEnumerable<ImmutableDataRow> rhs)
    {
        return rhs.Where(set.Contains);
    }
    
    private static IEnumerable<ImmutableDataRow> Intersect(IEnumerable<ImmutableDataRow> lhs, IEnumerable<ImmutableDataRow> rhs)
    {
        if (lhs is HashSet<ImmutableDataRow> lSet)
            return IntersectInternal(lSet, rhs);
        if (rhs is HashSet<ImmutableDataRow> rSet)
            return IntersectInternal(rSet, lhs);
        return IntersectInternal(lhs, rhs);
    }
    public IEnumerable<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock) where T : struct, DataIndexRegistry.IIndexersLock
    {
        var left = IAggregatorStream.Aggregate(predicateStream, indexersLock);
        var right = IAggregatorStream.Aggregate(predicateStream, indexersLock);
        return left switch
        {
            null when right == null => null,
            null => right,
            not null when right == null => left,
            _ => Intersect(left, right)
        };
    }
}