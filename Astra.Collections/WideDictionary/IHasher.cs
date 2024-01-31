using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Collections.WideDictionary;

public interface IHasher<in T>
{
    protected const long Seed = 42L;
    public ulong Hash(T item);
}

public abstract class UnmanagedTypeHasher<T> : IHasher<T> where T : unmanaged
{
    public ulong Hash(T item)
    {
        unsafe
        {
            return System.IO.Hashing.XxHash64.HashToUInt64(new ReadOnlySpan<byte>(&item, sizeof(T)), IHasher<T>.Seed);
        }
    }
}

public sealed class U8Hasher : UnmanagedTypeHasher<byte>;
public sealed class I8Hasher : UnmanagedTypeHasher<sbyte>;

public sealed class U16Hasher : UnmanagedTypeHasher<ushort>;
public sealed class I16Hasher : UnmanagedTypeHasher<short>;

public sealed class U32Hasher : UnmanagedTypeHasher<uint>;

public sealed class I32Hasher : UnmanagedTypeHasher<int>
{
    public static readonly I32Hasher Default = new();
}

public sealed class U64Hasher : IHasher<ulong>
{
    public ulong Hash(ulong item)
    {
        return item;
    }
}

public sealed class I64Hasher : IHasher<long>
{
    public ulong Hash(long item)
    {
        return unchecked((ulong)item);
    }
}

public sealed class U128Hasher : UnmanagedTypeHasher<UInt128>;
public sealed class I128Hasher : UnmanagedTypeHasher<Int128>;

public sealed class USHasher : UnmanagedTypeHasher<nuint>;
public sealed class ISHasher : UnmanagedTypeHasher<nint>;

public sealed class F16Hasher : UnmanagedTypeHasher<Half>;
public sealed class F32Hasher : UnmanagedTypeHasher<float>;
public sealed class F64Hasher : UnmanagedTypeHasher<double>;

public sealed class CharHasher : UnmanagedTypeHasher<char>;

public sealed class StringHasher : IHasher<string>
{
    public ulong Hash(string item)
    {
        return System.IO.Hashing.XxHash64.HashToUInt64(MemoryMarshal.Cast<char, byte>(item.AsSpan()), IHasher<string>.Seed);
    }
}

public sealed class BytesHasher : IHasher<byte[]>
{
    public ulong Hash(byte[] item)
    {
        return System.IO.Hashing.XxHash64.HashToUInt64(item, IHasher<byte[]>.Seed);
    }
}

public sealed class BytesMemoryHasher : IHasher<ReadOnlyMemory<byte>>
{
    public ulong Hash(ReadOnlyMemory<byte> item)
    {
        return System.IO.Hashing.XxHash64.HashToUInt64(item.Span, IHasher<byte[]>.Seed);
    }
}

public sealed class FallbackHasher<T> : IHasher<T>
{
    public static readonly FallbackHasher<T> Default = new();
    
    public ulong Hash(T? item)
    {
        return item == null ? 0UL : I32Hasher.Default.Hash(EqualityComparer<T>.Default.GetHashCode(item));
    }
}

file static class HasherHelper
{
    private static readonly Dictionary<Type, object> Hashers = new()
    {
        [typeof(byte)]      = new U8Hasher(),
        [typeof(sbyte)]     = new I8Hasher(),
        
        [typeof(ushort)]    = new U16Hasher(),
        [typeof(short)]     = new I16Hasher(),
        
        [typeof(uint)]      = new U32Hasher(),
        [typeof(int)]       = I32Hasher.Default,
        
        [typeof(ulong)]     = new U64Hasher(),
        [typeof(long)]      = new I64Hasher(),
        
        [typeof(UInt128)]   = new U128Hasher(),
        [typeof(Int128)]    = new I128Hasher(),
        
        [typeof(nuint)]     = new USHasher(),
        [typeof(nint)]      = new ISHasher(),
        
        [typeof(Half)]      = new F16Hasher(),
        [typeof(float)]     = new F32Hasher(),
        [typeof(double)]    = new F64Hasher(),
        
        [typeof(char)]       = new CharHasher(),
        [typeof(string)]     = new StringHasher(),
        [typeof(byte[])]     = new BytesHasher(),
        
        [typeof(ReadOnlyMemory<byte>)] = new BytesMemoryHasher(),
    };

    public static IHasher<T> GetHasher<T>()
    {
        if (!Hashers.TryGetValue(typeof(T), out var hasher))
            return FallbackHasher<T>.Default;
        return (IHasher<T>)hasher;
    }
}

public static class WideHasher<T>
{
    public static readonly IHasher<T> Default = HasherHelper.GetHasher<T>();
} 
