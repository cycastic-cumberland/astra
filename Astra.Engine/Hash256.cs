using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace Astra.Engine;

public readonly struct Hash256 : IEquatable<Hash256>
{
    public const int Size = 32;
    // Utilize YMM registers
    private readonly Vector256<ulong> _vector;

    private Hash256(Vector256<ulong> vector)
    {
        _vector = vector;
    }

    public static Hash256 Create(byte[] array)
    {
        var originalLength = array.Length;
        if (originalLength != 32) throw new ArgumentException(nameof(array));
        var vector = new Vector256<ulong>();
        unsafe
        {
            var vPtr = &vector;
            fixed (void* aPtr = &array[0])
            {
                Buffer.MemoryCopy(aPtr, vPtr, Size, Size);
            }
        }

        return new(vector);
    }

    public static Hash256 HashSha256(string str) => HashSha256(MemoryMarshal.AsBytes(str.AsSpan()));
    
    public static Hash256 HashSha256(ReadOnlySpan<byte> array) => Create(SHA256.HashData(array));

    public bool Equals(Hash256 other)
    {
        return _vector.Equals(other._vector);
    }

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