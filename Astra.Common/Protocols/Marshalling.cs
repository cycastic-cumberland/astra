using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Common.Protocols;

public static class Marshalling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T ToRef<T>(this Span<byte> bytes) where T : unmanaged
    {
        return ref Unsafe.As<byte, T>(ref bytes[0]);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T ToReadOnlyRef<T>(this ReadOnlySpan<byte> bytes) where T : unmanaged
    {
        return ref Unsafe.As<byte, T>(ref Unsafe.AsRef(in bytes[0]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> ToBytesSpan<T>(this ref T value) where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), Unsafe.SizeOf<T>());
    }
}