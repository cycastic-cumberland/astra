namespace Astra.Client;

public interface IAstraSerializable
{
    public void SerializeStream<TStream>(TStream writer) where TStream : Stream;
    public void DeserializeStream<TStream>(TStream reader) where TStream : Stream;
}