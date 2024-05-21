using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using Astra.Common.Protocols;
using Blake2Fast;

namespace Astra.Common.Hashes;

file static class Hash512Helpers
{
    public const int ZmmCapable = 2;
    public const int YmmCapable = 1;
    public static bool XmmCompare(Hash512 lhs, Hash512 rhs)
    {
        return lhs.Element0.Equals(rhs.Element0)
            && lhs.Element1.Equals(rhs.Element1)
            && lhs.Element2.Equals(rhs.Element2)
            && lhs.Element3.Equals(rhs.Element3);
    }

    public static bool YmmCompare(Hash512 lhs, Hash512 rhs)
    {
        var left = MemoryMarshal.CreateSpan(ref Unsafe.As<Hash512, Vector256<ulong>>(ref lhs), 2);
        var right = MemoryMarshal.CreateSpan(ref Unsafe.As<Hash512, Vector256<ulong>>(ref rhs), 2);
        return left[0].Equals(right[0])
               && left[1].Equals(right[1]);
    }

    public static bool ZmmCompare(Hash512 lhs, Hash512 rhs)
    {
        ref var left = ref Unsafe.As<Hash512, Vector512<ulong>>(ref lhs);
        ref var right = ref Unsafe.As<Hash512, Vector512<ulong>>(ref rhs);
        return left.Equals(right);
    }

    private static int SelectMode()
    {
        if (Vector512.IsHardwareAccelerated) return ZmmCapable;
        if (Vector256.IsHardwareAccelerated) return YmmCapable;
        return 0;
    }

    public static readonly int Mode = SelectMode();
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct Hash512 : IEquatable<Hash512>
{
    public const int Size = 64;
    [FieldOffset(0 * Hash128.Size)] 
    internal readonly Vector128<ulong> Element0;
    [FieldOffset(1 * Hash128.Size)]
    internal readonly Vector128<ulong> Element1;
    [FieldOffset(2 * Hash128.Size)]
    internal readonly Vector128<ulong> Element2;
    [FieldOffset(3 * Hash128.Size)]
    internal readonly Vector128<ulong> Element3;

    private static Hash512 ToHash512<T>(Vector512<T> vector)
    {
        return Unsafe.As<Vector512<T>, Hash512>(ref vector);
    }

    public static readonly Hash512 Zero = ToHash512(Vector512<ulong>.Zero);
    
    private static void CreateUnsafe(ReadOnlySpan<byte> span, out Hash512 hash)
    {
        hash = span.ToReadOnlyRef<Hash512>();
    }

    public static Hash512 Create(ReadOnlySpan<byte> span)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(span.Length, Size, nameof(span));
        CreateUnsafe(span, out var hash);
        return hash;
    }

    public static Hash512 HashBlake2B(ReadOnlySpan<byte> span)
    {
        Span<byte> buffer = stackalloc byte[Size];
        Blake2b.ComputeAndWriteHash(span, buffer);
        CreateUnsafe(buffer, out var hash);
        return hash;
    }

    public static Hash512 HashSha512(ReadOnlySpan<byte> span)
    {
        Span<byte> buffer = stackalloc byte[Size];
        SHA512.HashData(span, buffer);
        CreateUnsafe(buffer, out var hash);
        return hash;
    }

    public static Hash512 HashBlake2B(byte[] array) => HashBlake2B((ReadOnlySpan<byte>)array);

    public static Hash512 HashBlake2B(string str)
    {
        if (str.Length <= 24)
        {
            Span<byte> span = stackalloc byte[str.Length * 4];
            var length = Encoding.UTF8.GetBytes(str.AsSpan(), span);
            return HashBlake2B(span[..length]);
        }

        return HashBlake2B(Encoding.UTF8.GetBytes(str));
    }

    public bool Equals(Hash512 other)
    {
        return Hash512Helpers.Mode switch
        {
            Hash512Helpers.ZmmCapable => Hash512Helpers.ZmmCompare(this, other),
            Hash512Helpers.YmmCapable => Hash512Helpers.YmmCompare(this, other),
            _ => Hash512Helpers.XmmCompare(this, other)
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is Hash512 hash512 && Equals(hash512);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Element0, Element1, Element2, Element3);
    }

    public static bool operator ==(Hash512 left, Hash512 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Hash512 left, Hash512 right)
    {
        return !(left == right);
    }
    
    public override string ToString()
    {
        return ((ReadOnlySpan<byte>)Unsafe.AsRef(in this).ToBytesSpan()).ToHexString();
    }
}
