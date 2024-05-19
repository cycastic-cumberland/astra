using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Common.Data;

[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public struct ConnectionFlags
{
    public static class CompressionOptions
    {
        public const byte None = 0;
        public const byte GZip = 1;
        public const byte Deflate = 2;
        public const byte Brotli = 3;
        public const byte ZLib = 4;
        public const byte AlgorithmMask = byte.MaxValue - StrategyMask;
        public const byte StrategyMask = 192;
        public const byte Fastest = 1 << 7;
        public const byte SmallestSize = 1 << 6;
        public const byte Optimal = 0;
    }

    public static class General
    {
        public const byte IsCellBased = 1;
    }
    
    [FieldOffset(0)] 
    public byte Flags;
    [FieldOffset(1)] 
    private readonly byte _reserve1;
    [FieldOffset(2)] 
    private readonly byte _reserve2;
    [FieldOffset(3)] 
    public byte CompressionFlags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConnectionFlags From(uint value)
    {
        unsafe
        {
            return *(ConnectionFlags*)&value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConnectionFlags From(int value) => From(unchecked((uint)value));

    public byte CompressionAlgorithmRaw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked((byte)(CompressionFlags & CompressionOptions.AlgorithmMask));
    }

    public byte CompressionStrategyRaw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => unchecked((byte)(CompressionFlags & CompressionOptions.StrategyMask));
    }

    public Data.CompressionOptions Compression
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Data.CompressionOptions)CompressionFlags;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => CompressionFlags = unchecked((byte)value);
    }
    
    public bool IsCellBased
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Flags & General.IsCellBased) > 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Flags = unchecked((byte)(Flags & (value ? General.IsCellBased : ~General.IsCellBased)));
    }

    public uint Raw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                var self = this;
                return *(uint*)&self;
            }
        }
    }
}