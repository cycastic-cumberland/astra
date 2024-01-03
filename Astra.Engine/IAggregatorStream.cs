using System.Runtime.CompilerServices;

namespace Astra.Engine;

public class AggregatorNotSupported(string? msg = null) : NotSupportedException(msg);

public interface IAggregatorStream
{
    public HashSet<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataIndexRegistry.IIndexersLock;

    public static HashSet<ImmutableDataRow>? Aggregate<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataIndexRegistry.IIndexersLock
    {
        var type = predicateStream.ReadUInt();
        var ret = type switch
        {
            PredicateType.BinaryAndMask => new IntersectAggregator().ParseStream(predicateStream, indexersLock),
            PredicateType.BinaryOrMask => new UnionAggregator().ParseStream(predicateStream, indexersLock),
            PredicateType.UnaryMask => new UnaryAggregator().ParseStream(predicateStream, indexersLock),
            _ => throw new AggregateException($"Aggregator type not supported: {type}")
        };

        return ret;
    }
}

public static class AstraAggregatorHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<ImmutableDataRow>? Aggregate<T>(this Stream predicateStream, T indexersLock)
        where T : struct, DataIndexRegistry.IIndexersLock
    => IAggregatorStream.Aggregate(predicateStream, indexersLock);
}
