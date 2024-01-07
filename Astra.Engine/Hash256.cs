using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;

namespace Astra.Engine;

public readonly struct Hash256 : IEquatable<Hash256>
{
    public const int Size = 32;
    private readonly Vector256<ulong> _vector;
    
    public static unsafe Hash256 CreateUnsafe(ReadOnlySpan<byte> array)
    {
        if (array.Length != Size) throw new ArgumentException(nameof(array));
        fixed (void* ptr = &array[0])
        {
            // Maybe this would cause some problems with alignment...
            return *(Hash256*)ptr;
        }
    }

    public static Hash256 HashSha256(string str) => HashSha256(Encoding.UTF8.GetBytes(str));
    public static Hash256 HashSha256Fast(string str) => HashSha256(MemoryMarshal.AsBytes(str.AsSpan()));
    
    public static Hash256 HashSha256(ReadOnlySpan<byte> array) => CreateUnsafe(SHA256.HashData(array));

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

    public override string ToString()
    {
        unsafe
        {
            fixed (Hash256* self = &this)
            {
                var span = new ReadOnlySpan<byte>(&self->_vector, Size);
                return span.ToHexStringUpper();
            }
        }
    }
}