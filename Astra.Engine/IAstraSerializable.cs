namespace Astra.Engine;

public interface IAstraSerializable
{
    public void SerializeStream<TStream>(TStream writer) where TStream : Stream;
    public void DeserializeStream<TStream>(TStream reader) where TStream : Stream;
    
    public static IEnumerable<T> DeserializeStream<T, TStream>(TStream stream)
        where T : IAstraSerializable where TStream : Stream
    {
        try
        {
            var count = stream.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var value = Activator.CreateInstance<T>();
                value.DeserializeStream(stream);
                yield return value;
            }
        }
        finally
        {
            stream.Dispose();
        }
    }
}