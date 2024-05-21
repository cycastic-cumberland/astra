using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Engine;

public static class IntegerHelpers
{
    public static void ToSpan(this int value, Span<byte> span)
    {
        MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref value), sizeof(int))
            .CopyTo(span);
    }
    public static void ToSpan(this long value, Span<byte> span)
    {
        MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref value), sizeof(long))
            .CopyTo(span);
    }
}