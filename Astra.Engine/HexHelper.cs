namespace Astra.Engine;

internal static class HexHelper
{
    private const string HexDigitsUpper = "0123456789ABCDEF";
    private const string HexDigitsLower = "0123456789abcdef";
    public static string ToHexStringUpper(this ReadOnlySpan<byte> span)
    {
        Span<char> target = stackalloc char[span.Length * 2];
        var i = 0;
        foreach (var b in span)
        {
            // Bit-shift magic
            target[i] = HexDigitsUpper[(b >> 4) & 0x0F];
            target[i + 1] = HexDigitsUpper[b & 0x0F];
            i += 2;
        }
        return target.ToString();
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
        return target.ToString();
    }
    // Turn a byte span into a hex string - with minimal heap allocation
    public static string ToHexString(this ReadOnlySpan<byte> span, bool upperCase = true)
    {
        return upperCase ? span.ToHexStringUpper() : span.ToHexStringLower();
    }
}