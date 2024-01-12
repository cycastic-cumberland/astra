using Astra.Common;

namespace Astra.Engine;


public readonly struct UnaryAggregator : IAggregatorStream
{
    public HashSet<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock) where T : struct, DataIndexRegistry.IIndexersLock
    {
        var offset = predicateStream.ReadInt();
        return indexersLock.Read(offset, predicateStream, (tuple, stream) => tuple.handler?.Fetch(stream));
    }
}