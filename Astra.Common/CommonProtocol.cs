namespace Astra.Common;

public static class CommonProtocol
{
    public const uint AstraCommonVersion = 0x00010600U;
    public const int LongStringThreshold = 96;
    public const int ThreadLocalStreamDisposalThreshold = int.MaxValue / 4; // 512 MiB

    // This is so ridiculous...
    public static string GetCommonVersionString()
    {
        var version = AstraCommonVersion;
        Span<byte> span;
        unsafe
        {
            span = new Span<byte>(&version, sizeof(uint));
        }
        span.Reverse();

        return ((ReadOnlySpan<byte>)span).ToHexStringUpperSeparated();
    }
}