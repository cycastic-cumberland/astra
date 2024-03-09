using Astra.Common;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;

namespace Astra.Engine.Aggregator;


public readonly struct UnaryAggregator : IAggregatorStream
{
    public IEnumerable<ImmutableDataRow>? ParseStream<T>(Stream predicateStream, T indexersLock) where T : struct, DataRegistry.IIndexersLock
    {
        var offset = predicateStream.ReadInt();
        return indexersLock.Read(offset, predicateStream, (tuple, stream) => tuple.handler?.Fetch(stream)?.ToHashSet());
    }
}