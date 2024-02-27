using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Astra.Collections;

file static class UnsafeIntrinsicsBytesComparer
{
    public static unsafe bool XmmEquals(byte* l, byte* r, long length)
    {
        const int registerLength = 16;
        var alignedSize = length & ~(registerLength - 1);
        for (long i = 0; i < alignedSize; i += registerLength)
        {
            var left = Vector128.Create(new ReadOnlySpan<byte>(&l[i], registerLength));
            var right = Vector128.Create(new ReadOnlySpan<byte>(&r[i], registerLength));
            if (!left.Equals(right)) return false;
        }

        for (var i = alignedSize; i < length; i++)
        {
            if (l[i] != r[i]) return false;
        }

        return true;
    }
    
    public static unsafe bool YmmEquals(byte* l, byte* r, long length)
    {
        const int registerLength = 32;
        var alignedSize = length & ~(registerLength - 1);
        for (long i = 0; i < alignedSize; i += registerLength)
        {
            var left = Vector256.Create(new ReadOnlySpan<byte>(&l[i], registerLength));
            var right = Vector256.Create(new ReadOnlySpan<byte>(&r[i], registerLength));
            if (!left.Equals(right)) return false;
        }

        for (var i = alignedSize; i < length; i++)
        {
            if (l[i] != r[i]) return false;
        }

        return true;
    }
    
    public static unsafe bool ZmmEquals(byte* l, byte* r, long length)
    {
        const int registerLength = 64;
        var alignedSize = length & ~(registerLength - 1);
        for (long i = 0; i < alignedSize; i += registerLength)
        {
            var left = Vector512.Create(new ReadOnlySpan<byte>(&l[i], registerLength));
            var right = Vector512.Create(new ReadOnlySpan<byte>(&r[i], registerLength));
            if (!left.Equals(right)) return false;
        }

        for (var i = alignedSize; i < length; i++)
        {
            if (l[i] != r[i]) return false;
        }

        return true;
    }
}

file static class IntrinsicsBytesComparer
{
    private static unsafe bool XmmEquals(byte[] lhs, byte[] rhs)
    {
        if (lhs.LongLength != rhs.LongLength) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.XmmEquals(l, r, lhs.LongLength);
        }
    }

    private static unsafe bool YmmEquals(byte[] lhs, byte[] rhs)
    {
        if (lhs.LongLength != rhs.LongLength) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.YmmEquals(l, r, lhs.LongLength);
        }
    }

    private static unsafe bool ZmmEquals(byte[] lhs, byte[] rhs)
    {
        if (lhs.LongLength != rhs.LongLength) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.ZmmEquals(l, r, lhs.LongLength);
        }
    }

    private static Func<byte[], byte[], bool> SelectComparer()
    {
        if (Vector512.IsHardwareAccelerated) return ZmmEquals;
        if (Vector256.IsHardwareAccelerated) return YmmEquals;
        return XmmEquals;
    }

    public static readonly Func<byte[], byte[], bool> Compare = SelectComparer();
}

file static class IntrinsicsBytesMemoryComparer
{
    public static unsafe bool XmmEquals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length != rhs.Length) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.XmmEquals(l, r, lhs.Length);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool XmmEquals(ReadOnlyMemory<byte> lhs, ReadOnlyMemory<byte> rhs)
    {
        return XmmEquals(lhs.Span, rhs.Span);
    }
    
    public static unsafe bool YmmEquals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length != rhs.Length) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.YmmEquals(l, r, lhs.Length);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool YmmEquals(ReadOnlyMemory<byte> lhs, ReadOnlyMemory<byte> rhs)
    {
        return YmmEquals(lhs.Span, rhs.Span);
    }

    public static unsafe bool ZmmEquals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length != rhs.Length) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return UnsafeIntrinsicsBytesComparer.ZmmEquals(l, r, lhs.Length);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ZmmEquals(ReadOnlyMemory<byte> lhs, ReadOnlyMemory<byte> rhs)
    {
        return ZmmEquals(lhs.Span, rhs.Span);
    }

    private static Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, bool> SelectMemoryComparer()
    {
        if (Vector512.IsHardwareAccelerated) return ZmmEquals;
        if (Vector256.IsHardwareAccelerated) return YmmEquals;
        return XmmEquals;
    }
    

    public static readonly Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, bool> MemoryCompare = SelectMemoryComparer();
}


file abstract class BytesSpanComparator : IDefault<BytesSpanComparator>
{
    public abstract bool Compare(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs);

    private static BytesSpanComparator SelectComparator()
    {
        if (Vector512.IsHardwareAccelerated) return new ZmmBytesSpanComparator();
        if (Vector256.IsHardwareAccelerated) return new YmmBytesSpanComparator();
        return new XmmBytesSpanComparator();
    }

    public static BytesSpanComparator Default { get; } = SelectComparator();
}

file class XmmBytesSpanComparator : BytesSpanComparator
{
    public override bool Compare(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return IntrinsicsBytesMemoryComparer.XmmEquals(lhs, rhs);
    }
}

file class YmmBytesSpanComparator : BytesSpanComparator
{
    public override bool Compare(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return IntrinsicsBytesMemoryComparer.YmmEquals(lhs, rhs);
    }
}

file class ZmmBytesSpanComparator : BytesSpanComparator
{
    public override bool Compare(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return IntrinsicsBytesMemoryComparer.ZmmEquals(lhs, rhs);
    }
}

public static class BytesComparisonHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(byte[] lhs, byte[] rhs)
    {
        return lhs == rhs || IntrinsicsBytesComparer.Compare(lhs, rhs);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlyMemory<byte> lhs, ReadOnlyMemory<byte> rhs)
    {
        return lhs.Equals(rhs) || IntrinsicsBytesMemoryComparer.MemoryCompare(lhs, rhs);
    }

    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs == rhs || BytesSpanComparator.Default.Compare(lhs, rhs);
    }
}