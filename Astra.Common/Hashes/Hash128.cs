using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Astra.Common.Protocols;

namespace Astra.Common.Hashes;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Hash128 : IEquatable<Hash128>
{
    public const int Size = 16;
    
    private readonly UInt128 _vector;
    
    private Hash128(UInt128 vector) => _vector = vector;

    public static Hash128 CreateUnsafe(ReadOnlySpan<byte> array)
    {
        if (array.Length != 16) throw new ArgumentException(nameof(array));
        return array.ToReadOnlyRef<Hash128>();
    }
    public static Hash128 Create(UInt128 i128)
    {
        return new(i128);
    }

    public static readonly Hash128 Empty = new(UInt128.Zero);
    
    public static Hash128 HashMd5(ReadOnlySpan<byte> array) => CreateUnsafe(MD5.HashData(array));

    public static Hash128 HashMd5(string str) => HashMd5(Encoding.UTF8.GetBytes(str));
    
    public static Hash128 HashMd5Fast(string str) => HashMd5(MemoryMarshal.AsBytes(str.AsSpan()));
    
    public static Hash128 HashXx128(ReadOnlySpan<byte> array) => Create(System.IO.Hashing.XxHash128.HashToUInt128(array));

    public static Hash128 HashXx128(string str)
    {
        if (str.Length <= 24)
        {
            Span<byte> span = stackalloc byte[str.Length * 4];
            var length = Encoding.UTF8.GetBytes(str.AsSpan(), span);
            return HashXx128(span[..length]);
        }

        return HashXx128(Encoding.UTF8.GetBytes(str));
    }
    
    public void CopyTo(Stream stream)
    {
        stream.Write(Unsafe.AsRef(in this).ToBytesSpan());
    }
    
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
        return ((ReadOnlySpan<byte>)Unsafe.AsRef(in this).ToBytesSpan()).ToHexStringUpper();
    }
    
    public string ToStringLowerCase()
    {
        return ((ReadOnlySpan<byte>)Unsafe.AsRef(in this).ToBytesSpan()).ToHexStringLower();
    }

    public override string ToString() => ToStringUpperCase();
    public string ToString(bool upperCase) => upperCase ? ToStringUpperCase() : ToStringLowerCase();
}
