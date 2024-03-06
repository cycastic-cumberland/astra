namespace Astra.Common;

public static class HashHelpers
{
    private static unsafe Hash512 AddFillers(Hash256 hash256)
    {
        var result = Hash512.Zero;
        *(Hash256*)&result = hash256;
        return result;
    }
    
    public static Hash512 Hash(ReadOnlySpan<byte> span)
    {
        if (span.Length < 4096)
            return Hash512.HashBlake2B(span);
        var hash256 = Hash256.HashSha256(span);
        return AddFillers(hash256);
    }
}
