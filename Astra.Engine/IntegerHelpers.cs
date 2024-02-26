namespace Astra.Engine;

public static class IntegerHelpers
{
    public static unsafe void ToSpan(this int value, Span<byte> span)
    {
        new ReadOnlySpan<byte>(&value, sizeof(int)).CopyTo(span);
    }
    public static unsafe void ToSpan(this long value, Span<byte> span)
    {
        new ReadOnlySpan<byte>(&value, sizeof(long)).CopyTo(span);
    }
}