using Astra.Common;

namespace Astra.Engine.Indexers;

public interface IStreamResolver<out T>
{
    public static abstract T ConsumeStream(Stream stream);
}

public readonly struct IntegerStreamResolver : IStreamResolver<int>
{
    public static int ConsumeStream(Stream stream)
    {
        return stream.ReadInt();
    }
}

public readonly struct LongStreamResolver : IStreamResolver<long>
{
    public static long ConsumeStream(Stream stream)
    {
        return stream.ReadLong();
    }
}

public readonly struct SingleStreamResolver : IStreamResolver<float>
{
    public static float ConsumeStream(Stream stream)
    {
        return stream.ReadSingle();
    }
}

public readonly struct DoubleStreamResolver : IStreamResolver<double>
{
    public static double ConsumeStream(Stream stream)
    {
        return stream.ReadDouble();
    }
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

public readonly struct DecimalStreamResolver : IStreamResolver<decimal>
{
    public static decimal ConsumeStream(Stream stream)
    {
        return stream.ReadDecimal();
    }
}
