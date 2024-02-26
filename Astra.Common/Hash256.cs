using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;

namespace Astra.Common;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Hash256 : IEquatable<Hash256>
{
    public const int Size = 32;
    private readonly Vector256<ulong> _vector;

    private Hash256(Vector256<ulong> vector) => _vector = vector;
    
    private static unsafe ReadOnlySpan<byte> GetBytes(Hash256 hash)
    {
        return new(&hash, Size);
    }

    private static unsafe Hash256 CreateUnsafe(ReadOnlySpan<byte> array)
    {
        fixed (byte* ptr = &array[0])
        {
            return *(Hash256*)ptr;
        } 
    }
    
    public static Hash256 Create(ReadOnlySpan<byte> array)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length, Size, nameof(array));
        return CreateUnsafe(array);
    }

    public static Hash256 HashSha256(string str) => HashSha256(Encoding.UTF8.GetBytes(str));

    public static Hash256 HashSha256(ReadOnlySpan<byte> array)
    {
        Span<byte> span = stackalloc byte[Size];
        SHA256.HashData(array, span);
        return CreateUnsafe(span);
    }
    public static Hash256 HashSha256(byte[] array) => HashSha256((ReadOnlySpan<byte>)array);

    public unsafe void CopyTo(Span<byte> buffer)
    {
        var vec = _vector;
        var span = new ReadOnlySpan<byte>(&vec, Size);
        span.CopyTo(buffer);
    }
    
    public bool Equals(Hash256 other)
    {
        return _vector.Equals(other._vector);
    }

    public static bool Compare(Hash256 lhs, Hash256 rhs) => lhs.Equals(rhs);

    public override bool Equals(object? obj)
    {
        return obj is Hash256 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _vector.GetHashCode();
    }

    public static bool operator ==(Hash256 left, Hash256 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Hash256 left, Hash256 right)
    {
        return !(left == right);
    }

    public override unsafe string ToString()
    {
        var vec = _vector;
        var span = new ReadOnlySpan<byte>(&vec, Size);
        return span.ToHexString();
    }
}