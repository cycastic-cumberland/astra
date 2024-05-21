using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using Astra.Common.Protocols;

namespace Astra.Common.Hashes;

file static class Hash256Helpers
{
    public const int YmmCapable = 1;
    public static bool XmmCompare(Hash256 lhs, Hash256 rhs)
    {
        return lhs.Element0.Equals(rhs.Element0) && lhs.Element1.Equals(rhs.Element1);
    }
    
    public static bool YmmCompare(Hash256 lhs, Hash256 rhs)
    {
        ref var left = ref Unsafe.As<Hash256, Vector256<ulong>>(ref lhs);
        ref var right = ref Unsafe.As<Hash256, Vector256<ulong>>(ref rhs);
        return left.Equals(right);
    }

    private static int SelectMode()
    {
        if (Vector256.IsHardwareAccelerated) return YmmCapable;
        return 0;
    }
    
    public static readonly int Mode = SelectMode();
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct Hash256 : IEquatable<Hash256>
{
    public const int Size = 32;
    [FieldOffset(0 * Hash128.Size)] 
    internal readonly Vector128<ulong> Element0;
    [FieldOffset(1 * Hash128.Size)]
    internal readonly Vector128<ulong> Element1;

    private static Hash256 ToHash512<T>(Vector256<T> vector)
    {
        return Unsafe.As<Vector256<T>, Hash256>(ref vector);
    }

    public static readonly Hash256 Zero = ToHash512(Vector256<ulong>.Zero);
    
    private static void CreateUnsafe(ReadOnlySpan<byte> array, out Hash256 hash)
    {
        hash = array.ToReadOnlyRef<Hash256>();
    }
    
    public static Hash256 Create(ReadOnlySpan<byte> array)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length, Size, nameof(array));
        CreateUnsafe(array, out var hash);
        return hash;
    }

    public static Hash256 HashSha256(string str) => HashSha256(Encoding.UTF8.GetBytes(str));

    public static Hash256 HashSha256(ReadOnlySpan<byte> array)
    {
        Span<byte> span = stackalloc byte[Size];
        SHA256.HashData(array, span);
        CreateUnsafe(span, out var hash);
        return hash;
    }

    public static Hash256 HashSha256(ReadOnlyMemory<byte> array) => HashSha256(array.Span);
    
    public static Hash256 HashSha256(byte[] array) => HashSha256((ReadOnlySpan<byte>)array);

    public void CopyTo(Span<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, Size);
        Unsafe.AsRef(in this).ToBytesSpan().CopyTo(buffer);
    }
    
    public bool Equals(Hash256 other)
    {
        return Hash256Helpers.Mode switch
        {
            Hash256Helpers.YmmCapable => Hash256Helpers.YmmCompare(this, other),
            _ => Hash256Helpers.XmmCompare(this, other)
        };
    }

    public static bool Compare(Hash256 lhs, Hash256 rhs) => lhs.Equals(rhs);

    public override bool Equals(object? obj)
    {
        return obj is Hash256 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Element0, Element1);
    }

    public static bool operator ==(Hash256 left, Hash256 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Hash256 left, Hash256 right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return ((ReadOnlySpan<byte>)Unsafe.AsRef(in this).ToBytesSpan()).ToHexString();
    }
}