namespace Astra.Common;

public static class BytesComparisonHelper
{
    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs.SequenceEqual(rhs);
    }

    public static int CompareBetween(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs.SequenceCompareTo(rhs);
    }
}