using Astra.Common.Data;
using Astra.Common.StreamUtils;

namespace Astra.Common.Serializable;

public struct FlexSerializable<T> : IAstraSerializable
{
    private T _target;

    public T Target
    {
        get => _target;
        set => _target = value;
    }
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper
    {
        DynamicSerializable.GetDefaultSerializer<T>().SerializeStream(writer, _target);
    }

    public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper
    {
        _target = DynamicSerializable.GetDefaultSerializer<T>().DeserializeStream(reader);
    }
}