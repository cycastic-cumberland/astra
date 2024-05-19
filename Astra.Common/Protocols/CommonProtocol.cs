using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.StreamUtils;

namespace Astra.Common.Protocols;

public static class CommonProtocol
{
    public const int SignatureSizeBit = 2048;
    public const int SignatureSize = 2048 / 8;
    public const uint AstraCommonVersion = 0x00020300U;
    public const int LongStringThreshold = 96;
    public const uint PublicKeyChallengeLength = 64;
    public const int SaltLength = 16;
    public const int ThreadLocalStreamDisposalThreshold = int.MaxValue / 4; // 512 MiB

    public const byte HasRow = 1;
    public const byte ChainedFlag = HasRow;
    public const byte EndOfSetFlag = 0;
    
    // This is so ridiculous...
    public static string ToAstraCommonVersion(this uint value)
    {
        var version = ReverseStreamWrapper.ReverseEndianness(value);
        Span<byte> span = stackalloc byte[sizeof(uint)];
        version.ToBytes(span);

        return ((ReadOnlySpan<byte>)span).ToHexStringUpperSeparated();
    }
    public static string GetCommonVersionString()
    {
        return AstraCommonVersion.ToAstraCommonVersion();
    }
    public static BytesCluster CombineSalt(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt)
    {
        var cluster = BytesCluster.Rent(password.Length + salt.Length);
        try
        {
            salt.CopyTo(cluster.Writer);
            password.CopyTo(cluster.Writer[salt.Length..]);
            return cluster;
        }
        catch
        {
            cluster.Dispose();
            throw;
        }
    }
}