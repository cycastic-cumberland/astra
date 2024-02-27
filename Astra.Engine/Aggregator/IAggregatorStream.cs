using System.Runtime.CompilerServices;
using Astra.Common;
using Astra.Engine.Data;

namespace Astra.Engine.Aggregator;

public class AggregatorNotSupported(string? msg = null) : NotSupportedException(msg);

public interface IAggregatorStream
{
    public IEnumerable<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock;

    public static IEnumerable<ImmutableDataRow>? Aggregate<T>(Stream predicateStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock
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
        where T : struct, DataRegistry.IIndexersLock
    => IAggregatorStream.Aggregate(predicateStream, indexersLock)?.ToHashSet();

    public static void AggregateStream<T>(this Stream predicateStream, Stream outStream, T indexersLock)
        where T : struct, DataRegistry.IIndexersLock
    {
        var result = predicateStream.Aggregate(indexersLock);
        if (result == null)
        {
            outStream.WriteValue(0);
            return;
        }
        outStream.WriteValue(result.Count);
        var resolverCount = indexersLock.Count;
        foreach (var row in result)
        {
            for (var i = 0; i < resolverCount; i++)
            {
                indexersLock.Read(i, (outStream, row), (tuple, enclosed) =>
                {
                    tuple.resolver.Serialize(enclosed.outStream, enclosed.row);
                });
            }
        }
    }
}
