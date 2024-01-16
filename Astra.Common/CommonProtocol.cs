namespace Astra.Common;

public static class CommonProtocol
{
    public const uint AstraCommonVersion = 0x00010900U;
    public const int LongStringThreshold = 96;
    public const uint PublicKeyChallengeLength = 64;
    public const int SaltLength = 16;
    public const int ThreadLocalStreamDisposalThreshold = int.MaxValue / 4; // 512 MiB

    // This is so ridiculous...
    public static string ToAstraCommonVersion(this uint value)
    {
        var version = ReverseStreamWrapper.ReverseEndianness(value);
        Span<byte> span;
        unsafe
        {
            span = new Span<byte>(&version, sizeof(uint));
        }

        return ((ReadOnlySpan<byte>)span).ToHexStringUpperSeparated();
    }
    public static string GetCommonVersionString()
    {
        return AstraCommonVersion.ToAstraCommonVersion();
    }
    public static byte[] CombineSalt(byte[] password, byte[] salt)
    {
        var ret = new byte[password.Length + salt.Length];
        Buffer.BlockCopy(salt, 0, ret, 0, salt.Length);
        Buffer.BlockCopy(password, 0, ret, salt.Length, password.Length);
        return ret;
    }
}