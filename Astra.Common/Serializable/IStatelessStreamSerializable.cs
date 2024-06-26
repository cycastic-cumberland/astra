using Astra.Common.StreamUtils;

namespace Astra.Common.Serializable;

public interface IStatelessStreamSerializable<T>
{
    public void SerializeStream<TStream>(TStream writer, T value) where TStream : IStreamWrapper;
    public T DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper;
}
