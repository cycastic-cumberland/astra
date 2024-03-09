namespace Astra.Common.Hashes;

public static class HexHelper
{
    private const string HexDigitsUpper = "0123456789ABCDEF";
    private const string HexDigitsLower = "0123456789abcdef";
    public static string ToHexStringUpper(this ReadOnlySpan<byte> span)
    {
        // Use stack memory to store the buffer, which is faster but increase the chance of stack overflow
        Span<char> target = stackalloc char[span.Length * 2];
        var i = 0;
        foreach (var b in span)
        {
            // Bit-shift magic
            target[i] = HexDigitsUpper[(b >> 4) & 0x0F];
            target[i + 1] = HexDigitsUpper[b & 0x0F];
            i += 2;
        }
        return new string(target);
    }
    public static string ToHexStringUpperSeparated(this ReadOnlySpan<byte> span, char separator = '.')
    {
        Span<char> target = stackalloc char[span.Length * 2 + span.Length - 1];
        var i = 0;
        foreach (var b in span)
        {
            // Bit-shift magic
            target[i] = HexDigitsUpper[(b >> 4) & 0x0F];
            target[i + 1] = HexDigitsUpper[b & 0x0F];
            if (i + 3 < target.Length)
            {
                target[i + 2] = separator;
                i++;
            }
            i += 2;
        }
        return new string(target);
    }
    public static string ToHexStringLower(this ReadOnlySpan<byte> span)
    {
        Span<char> target = stackalloc char[span.Length * 2];
        var i = 0;
        foreach (var b in span)
        {
            target[i] = HexDigitsLower[(b >> 4) & 0x0F];
            target[i + 1] = HexDigitsLower[b & 0x0F];
            i += 2;
        }
        return new string(target);
    }
    // Turn a byte span into a hex string - with minimal heap allocation
    public static string ToHexString(this ReadOnlySpan<byte> span, bool upperCase = true)
    {
        return upperCase ? span.ToHexStringUpper() : span.ToHexStringLower();
    }

    public static string ToHexString(this byte[] array, bool upperCase = true)
    {
        return new ReadOnlySpan<byte>(array).ToHexString(upperCase);
    }
}