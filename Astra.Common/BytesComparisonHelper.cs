using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Astra.Common;

file static class IntrinsicsBytesComparer
{
    private static bool Equals(ref readonly byte l, ref readonly byte r, long length)
    {
        const int v128Length = sizeof(long) * 2;
        const int v256Length = v128Length * 2;
        const int v512Length = v256Length * 2;
        while (length > 0)
        {
            bool result;
            int processed;
            switch (length)
            {
                case >= v512Length when Vector512.IsHardwareAccelerated:
                {
                    ref var left = ref Unsafe.As<byte, Vector512<ulong>>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, Vector512<ulong>>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, v512Length);
                    break;
                }
                case >= v256Length when Vector256.IsHardwareAccelerated:
                {
                    ref var left = ref Unsafe.As<byte, Vector256<ulong>>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, Vector256<ulong>>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, v256Length);
                    break;
                }
                case >= v128Length when Vector128.IsHardwareAccelerated:
                {
                    ref var left = ref Unsafe.As<byte, Vector128<ulong>>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, Vector128<ulong>>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, v128Length);
                    break;
                }
                case >= sizeof(ulong):
                {
                    ref var left = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, sizeof(ulong));
                    break;
                }
                case >= sizeof(uint):
                {
                    ref var left = ref Unsafe.As<byte, uint>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, uint>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, sizeof(uint));
                    break;
                }
                case >= sizeof(ushort):
                {
                    ref var left = ref Unsafe.As<byte, ushort>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, ushort>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left == right, sizeof(ushort));
                    break;
                }
                default:
                {
                    ref var left = ref Unsafe.AsRef(in l);
                    ref var right = ref Unsafe.AsRef(in r);
                    (result, processed) = (left == right, 1);
                    break;
                }
            }
            if (!result) return false;
            l = ref Unsafe.Add(ref Unsafe.AsRef(in l), (nint)processed);
            r = ref Unsafe.Add(ref Unsafe.AsRef(in r), (nint)processed);
            length -= processed;
        }

        return true;
    }
    
    private static int CompareBetween(ref readonly byte l, ref readonly byte r, long length)
    {
        const int v128Length = sizeof(long) * 2;
        while (length > 0)
        {
            int result;
            int processed;
            switch (length)
            {
                case >= v128Length:
                {
                    ref var left = ref Unsafe.As<byte, UInt128>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, UInt128>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left.CompareTo(right), v128Length);
                    break;
                }
                case >= sizeof(ulong):
                {
                    ref var left = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, ulong>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left.CompareTo(right), sizeof(ulong));
                    break;
                }
                case >= sizeof(uint):
                {
                    ref var left = ref Unsafe.As<byte, uint>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, uint>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left.CompareTo(right), sizeof(uint));
                    break;
                }
                case >= sizeof(ushort):
                {
                    ref var left = ref Unsafe.As<byte, ushort>(ref Unsafe.AsRef(in l));
                    ref var right = ref Unsafe.As<byte, ushort>(ref Unsafe.AsRef(in r));
                    (result, processed) = (left.CompareTo(right), sizeof(ushort));
                    break;
                }
                default:
                {
                    ref var left = ref Unsafe.AsRef(in l);
                    ref var right = ref Unsafe.AsRef(in r);
                    (result, processed) = (left.CompareTo(right), 1);
                    break;
                }
            }
            if (result != 0) return result;
            l = ref Unsafe.Add(ref Unsafe.AsRef(in l), (nint)processed);
            r = ref Unsafe.Add(ref Unsafe.AsRef(in r), (nint)processed);
            length -= processed;
        }

        return 0;
    }

    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length != rhs.Length) return false;
        return lhs.Length == 0 || Equals(in lhs[0], in rhs[0], lhs.Length);
    }

    public static int CompareBetween(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        if (lhs.Length < rhs.Length) return -1;
        if (lhs.Length > rhs.Length) return 1;
        return lhs.Length == 0 ? 0 : CompareBetween(in lhs[0], in rhs[0], lhs.Length);
    }
}


public static class BytesComparisonHelper
{
    public static bool Equals(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return lhs == rhs || IntrinsicsBytesComparer.Equals(lhs, rhs);
    }

    public static int CompareBetween(ReadOnlySpan<byte> lhs, ReadOnlySpan<byte> rhs)
    {
        return IntrinsicsBytesComparer.CompareBetween(lhs, rhs);
    }
}