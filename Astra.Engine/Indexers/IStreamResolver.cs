using Astra.Common;

namespace Astra.Engine.Indexers;

public interface IStreamResolver<out T>
{
    public static abstract T ConsumeStream(Stream stream);
}

public readonly struct BytesStreamResolver : IStreamResolver<ComparableBytesMemory>
{
    public static ComparableBytesMemory ConsumeStream(Stream stream)
    {
        return stream.ReadSequence();
    }
}

public readonly struct StringWrapperStreamResolver : IStreamResolver<StringWrapper>
{
    public static StringWrapper ConsumeStream(Stream stream)
    {
        return stream.ReadString();
    }
}
