namespace Astra.Common;

public static class CommonProtocol
{
    public const uint AstraCommonVersion = 0x00010800U;
    public const int LongStringThreshold = 96;
    public const uint PublicKeyChallengeLength = 64;
    public const int ThreadLocalStreamDisposalThreshold = int.MaxValue / 4; // 512 MiB

    // This is so ridiculous...
    public static string GetCommonVersionString()
    {
        var version = ReverseStreamWrapper.ReverseEndianness(AstraCommonVersion);
        Span<byte> span;
        unsafe
        {
            span = new Span<byte>(&version, sizeof(uint));
        }

        return ((ReadOnlySpan<byte>)span).ToHexStringUpperSeparated();
    }
}