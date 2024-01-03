using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;

namespace Astra.Engine;

public readonly struct Hash128 : IEquatable<Hash128>
{
    public const int Size = 16;
    // Utilize XMM registers
    private readonly Vector128<ulong> _vector;

    private Hash128(Vector128<ulong> vector) => _vector = vector;

    public static Hash128 Create(ReadOnlySpan<byte> array)
    {
        var originalLength = array.Length;
        if (originalLength != 16) throw new ArgumentException(nameof(array));
        return new(MemoryMarshal.Read<Vector128<ulong>>(array));
    }
    
    public static Hash128 Create(UInt128 i128)
    {
        unsafe
        {
            void* ptr = &i128;
            return new(MemoryMarshal.Read<Vector128<ulong>>(new ReadOnlySpan<byte>(ptr, Size)));
        }
    }

    public static Hash128 Empty => new(Vector128<ulong>.Zero);
    
    public static Hash128 HashMd5(ReadOnlySpan<byte> array) => Create(MD5.HashData(array));

    public static Hash128 HashMd5(string str) => HashMd5(Encoding.UTF8.GetBytes(str));
    
    public static Hash128 HashMd5Fast(string str) => HashMd5(MemoryMarshal.AsBytes(str.AsSpan()));

    public static Hash128 HashMurmur3(ReadOnlySpan<byte> array) => MurmurHashInterop.MurmurHash3_x64_128(array);
    
    public static Hash128 HashMurmur3(string str) => MurmurHashInterop.MurmurHash3_x64_128(Encoding.UTF8.GetBytes(str));
    
    public static Hash128 HashMurmur3Fast(string str) => MurmurHashInterop.MurmurHash3_x64_128(MemoryMarshal.AsBytes(str.AsSpan()));

    public static Hash128 HashXx128(ReadOnlySpan<byte> array) => Create(System.IO.Hashing.XxHash128.HashToUInt128(array));
    
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

    public override string ToString()
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
}
