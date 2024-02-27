using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Astra.Collections;

file static class IntrinsicsBytesComparer
{
    private static unsafe bool Equals(byte* l, byte* r, long length)
    {
        while (length > 0)
        {
            bool result;
            int processed;
            switch (length)
            {
                case >= 64 when Vector128.IsHardwareAccelerated:
                {
                    var left = *(Vector512<ulong>*)l;
                    var right = *(Vector512<ulong>*)r;
                    (result, processed) = (left == right, sizeof(ulong) * sizeof(ulong));
                    break;
                }
                case >= 32 when Vector128.IsHardwareAccelerated:
                {
                    var left = *(Vector256<ulong>*)l;
                    var right = *(Vector256<ulong>*)r;
                    (result, processed) = (left == right, sizeof(ulong) * sizeof(uint));
                    break;
                }
                case >= 16 when Vector128.IsHardwareAccelerated:
                {
                    var left = *(Vector128<ulong>*)l;
                    var right = *(Vector128<ulong>*)r;
                    (result, processed) = (left == right, sizeof(ulong) * sizeof(ushort));
                    break;
                }
                case >= sizeof(ulong):
                {
                    var left = *(ulong*)l;
                    var right = *(ulong*)r;
                    (result, processed) = (left == right, sizeof(ulong));
                    break;
                }
                case >= sizeof(uint):
                {
                    var left = *(uint*)l;
                    var right = *(uint*)r;
                    (result, processed) = (left == right, sizeof(uint));
                    break;
                }
                case >= sizeof(ushort):
                {
                    var left = *(ushort*)l;
                    var right = *(ushort*)r;
                    (result, processed) = (left == right, sizeof(ushort));
                    break;
                }
                default:
                {
                    (result, processed) = (*l == *r, 1);
                    break;
                }
            }
            if (!result) return false;
            l = &l[processed];
            r = &r[processed];
            length -= processed;
        }

        return true;
    }
    public static unsafe bool Compare(byte[] lhs, byte[] rhs)
    {
        if (lhs.LongLength != rhs.LongLength) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return Equals(l, r, lhs.LongLength);
        }
    }
    public static unsafe bool Compare(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length != rhs.Length) return false;
        fixed (byte* l = &lhs[0], r = &rhs[0])
        {
            return Equals(l, r, lhs.Length);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Compare(ReadOnlyMemory<byte> lhs, ReadOnlyMemory<byte> rhs)
    {
        return Compare(lhs.Span, rhs.Span);
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
        return lhs.Equals(rhs) || IntrinsicsBytesComparer.Compare(lhs, rhs);
    }

    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs == rhs || IntrinsicsBytesComparer.Compare(lhs, rhs);
    }
}