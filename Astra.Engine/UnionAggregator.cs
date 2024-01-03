namespace Astra.Engine;

public readonly struct UnionAggregator : IAggregatorStream
{
    private static HashSet<ImmutableDataRow> Union(HashSet<ImmutableDataRow> lhs, HashSet<ImmutableDataRow> rhs)
    {
        var cloned = new HashSet<ImmutableDataRow>(lhs);
        cloned.UnionWith(rhs);
        return cloned;
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
            _ => Union(left, right)
        };
    }
}