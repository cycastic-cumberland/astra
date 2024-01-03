namespace Astra.Engine;

public readonly struct IntersectAggregator : IAggregatorStream
{
    private static HashSet<ImmutableDataRow> Intersect(HashSet<ImmutableDataRow> lhs, HashSet<ImmutableDataRow> rhs)
    {
        HashSet<ImmutableDataRow> ret = new();
        foreach (var row in lhs)
        {
            if (rhs.Contains(row)) ret.Add(row);
        }

        return ret;
    }
    public HashSet<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock) where T : struct, DataIndexRegistry.IIndexersLock
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