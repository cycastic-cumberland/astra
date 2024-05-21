using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Astra.Common;

public static class BytesComparisonHelper
{
    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs.SequenceEqual(rhs);
    }
}