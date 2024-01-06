using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Astra.Engine;

public readonly struct Hash128 : IEquatable<Hash128>
{
    public const int Size = 16;
    
    private readonly UInt128 _vector;
    
    private Hash128(UInt128 vector) => _vector = vector;

    public static Hash128 Create(ReadOnlySpan<byte> array)
    {
        if (array.Length != 16) throw new ArgumentException(nameof(array));
        unsafe
        {
            fixed (void* ptr = &array[0])
            {
                // Type punning magic
                return new(*(UInt128*)ptr);
            }
        }
    }
    public static Hash128 Create(UInt128 i128)
    {
        return new(i128);
    }

    public static readonly Hash128 Empty = new(UInt128.Zero);
    
    public static Hash128 HashMd5(ReadOnlySpan<byte> array) => Create(MD5.HashData(array));

    public static Hash128 HashMd5(string str) => HashMd5(Encoding.UTF8.GetBytes(str));
    
    public static Hash128 HashMd5Fast(string str) => HashMd5(MemoryMarshal.AsBytes(str.AsSpan()));

    public static Hash128 HashXx128(ReadOnlySpan<byte> array) => Create(System.IO.Hashing.XxHash128.HashToUInt128(array));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Hash128 other)
    {
        return _vector.Equals(other._vector);
    }

    public override bool Equals(object? obj)
    {
        return obj is Hash128 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _vector.GetHashCode();
    }

    public static bool operator ==(Hash128 left, Hash128 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Hash128 left, Hash128 right)
    {
        return !(left == right);
    }

    public string ToStringUpperCase()
    {
        unsafe
        {
            fixed (Hash128* self = &this)
            {
                var span = new ReadOnlySpan<byte>(&self->_vector, Size);
                return span.ToHexStringUpper();
            }
        }
    }
    
    public string ToStringLowerCase()
    {
        unsafe
        {
            fixed (Hash128* self = &this)
            {
                var span = new ReadOnlySpan<byte>(&self->_vector, Size);
                return span.ToHexStringLower();
            }
        }
    }

    public override string ToString() => ToStringUpperCase();
    public string ToString(bool upperCase) => upperCase ? ToStringUpperCase() : ToStringLowerCase();
}
