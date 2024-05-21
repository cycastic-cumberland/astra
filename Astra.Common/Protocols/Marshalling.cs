using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Common.Protocols;

public static class Marshalling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MarshalTo<T>(this ReadOnlySpan<byte> bytes) where T : unmanaged
    {
        return Unsafe.As<byte, T>(ref Unsafe.AsRef(in bytes[0]));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToBytes<T>(this T value, Span<byte> toSpan) where T : unmanaged
    {
        var fromSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), Unsafe.SizeOf<T>());
        fromSpan.CopyTo(toSpan);
    }
}