namespace Astra.Common;

public interface IAstraSerializable
{
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper;
    public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper;

    private static IEnumerator<T> DeserializeStreamInternal<T, TStream>(TStream stream)
        where T : IAstraSerializable where TStream : IStreamWrapper
    {
        var count = stream.LoadInt();
        for (var i = 0; i < count; i++)
        {
            var value = Activator.CreateInstance<T>();
            value.DeserializeStream(stream);
            yield return value;
        }
    }
    public static IEnumerable<T> DeserializeStream<T, TStream>(TStream stream, bool isReversed)
        where T : IAstraSerializable where TStream : Stream
    {
        try
        {
            if (isReversed)
            {
                using var cursor = DeserializeStreamInternal<T, ReverseStreamWrapper>(new(stream));
                while (cursor.MoveNext())
                {
                    yield return cursor.Current;
                }
            }
            else
            {
                using var cursor = DeserializeStreamInternal<T, ForwardStreamWrapper>(new(stream));
                while (cursor.MoveNext())
                {
                    yield return cursor.Current;
                }
            }
        }
        finally
        {
            stream.Dispose();
        }
    }
}