using Astra.Common.StreamUtils;

namespace Astra.Common.Data;

public interface IAstraSerializable
{
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper;
    public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper;
}