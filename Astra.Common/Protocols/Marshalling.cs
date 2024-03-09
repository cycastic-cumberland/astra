using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Common.Protocols;

public static class Marshalling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T MarshalTo<T>(this ReadOnlySpan<byte> bytes) where T : unmanaged
    {
        return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(bytes));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToBytes<T>(this T value, Span<byte> toSpan) where T : unmanaged
    {
        unsafe
        {
            var fromSpan = new Span<byte>(&value, sizeof(T));
            fromSpan.CopyTo(toSpan);
        }
    }
}