using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Astra.Common.Data;
using Astra.Common.Protocols;

namespace Astra.Common.StreamUtils;

public readonly struct ReverseStreamWrapper(Stream stream) : IStreamWrapper
{
    private Stream Debug => stream;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseEndianness(uint value)
    {
        return ((value >> 24) & 0xFF) |
                ((value >> 8) & 0xFF00) |
                ((value << 8) & 0xFF0000) |
                ((value << 24) & 0xFF000000U);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDWordUnmanaged<T>(Stream writer, T value) where T : unmanaged
    {
        const int size = sizeof(uint);
        var bytes = ReverseEndianness(Unsafe.As<T, uint>(ref value));
        writer.Write(MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref bytes), size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadDWordUnmanaged(Stream reader)
    {
        const int size = sizeof(uint);
        var bytes = 0U;
        reader.ReadExactly(MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref bytes), size));
        return ReverseEndianness(bytes);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReverseEndianness(ulong value)
    {
        return ((value >> 56) & 0xFF) |
                ((value >> 40) & 0xFF00) |
                ((value >> 24) & 0xFF0000) |
                ((value >> 8) & 0xFF000000) |
                ((value << 8) & 0xFF00000000) |
                ((value << 24) & 0xFF0000000000) |
                ((value << 40) & 0xFF000000000000) |
                ((value << 56) & 0xFF00000000000000UL);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteQWordUnsafe<T>(Stream writer, T value) where T : unmanaged
    {
        const int size = sizeof(ulong);
        var bytes = ReverseEndianness(Unsafe.As<T, ulong>(ref value));
        writer.Write(MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref bytes), size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadQWordUnsafe(Stream reader)
    {
        const int size = sizeof(ulong);
        var bytes = 0UL;
        reader.ReadExactly(MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref bytes), size));
        return ReverseEndianness(bytes);
    }
    
    public void SaveValue(byte value)
    {
        stream.WriteValue(value);
    }
    
    public void SaveValue(int value)
    {
        WriteDWordUnmanaged(stream, value);
    }

    public ValueTask SaveValueAsync(int value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(uint value)
    {
        WriteDWordUnmanaged(stream, value);
    }

    public ValueTask SaveValueAsync(uint value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(long value)
    {
        WriteQWordUnsafe(stream, value);
    }

    public ValueTask SaveValueAsync(long value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(ulong value)
    {
        WriteQWordUnsafe(stream, value);
    }

    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(float value)
    {
        WriteDWordUnmanaged(stream, value);
    }

    public ValueTask SaveValueAsync(float value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(double value)
    {
        WriteQWordUnsafe(stream, value);
    }

    public ValueTask SaveValueAsync(double value, CancellationToken cancellationToken = default)
    {
        var bytes = BitConverter.GetBytes(value);
        var span = bytes.AsSpan();
        span.Reverse();
        bytes = span.ToArray();
        return stream.WriteAsync(bytes, cancellationToken);
    }

    private void WriteShortString(ReadOnlySpan<char> value)
    {
        // Reference: https://www.rfc-editor.org/rfc/rfc3629 (Section 3. UTF-8 definition)
        // TLDR: The max number of bytes per UTF-8 character is 4 
        Span<byte> bytes = stackalloc byte[value.Length * 4];
        var written = Encoding.UTF8.GetBytes(value, bytes);
        SaveValue(written);
        stream.Write(bytes[..written]);
    }
    
    private void WriteLongString(ReadOnlySpan<char> value)
    {
        using var buffer = BytesCluster.Rent(value.Length * 4);
        var bytes = buffer.Writer;
        var written = Encoding.UTF8.GetBytes(value, bytes);
        SaveValue(written);
        stream.Write(bytes[..written]);
    }

    public void SaveValue(string value)
    {
        if (value == null) throw new ArgumentException(nameof(value));
        if (value.Length < CommonProtocol.LongStringThreshold) WriteShortString(value);
        else WriteLongString(value);
    }

    public async ValueTask SaveValueAsync(string value, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        await SaveValueAsync(bytes.Length, cancellationToken);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    public void SaveValue(StringRef value)
    {
        if (value.Length < CommonProtocol.LongStringThreshold) WriteShortString(value);
        else WriteLongString(value);
    }

    public void SaveValue(byte[] value)
    {
        SaveValue(value.LongLength);
        stream.Write(value);
    }

    public void SaveValue(ReadOnlySpan<byte> value)
    {
        SaveValue((long)value.Length);
        stream.Write(value);
    }
    public async ValueTask SaveValueAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(value.LongLength, cancellationToken);
        await stream.WriteAsync(value, cancellationToken);
    }

    public void SaveValue(BytesCluster value)
    {
        SaveValue(value.LongLength);
        stream.Write(value.Reader);
    }

    public async ValueTask SaveValueAsync(BytesCluster value, CancellationToken cancellationToken = default)
    {
        await SaveValueAsync(value.LongLength, cancellationToken);
        await stream.WriteAsync(value.ReaderMemory, cancellationToken);
    }
    
    public byte LoadByte()
    {
        Span<byte> span = stackalloc byte[1];
        stream.ReadExactly(span);
        return span[0];
    }

    public int LoadInt()
    {
        return unchecked((int)ReadDWordUnmanaged(stream));
    }

    public uint LoadUInt()
    {
        return ReadDWordUnmanaged(stream);
    }

    public long LoadLong()
    {
        return unchecked((long)ReadQWordUnsafe(stream));
    }

    public ulong LoadULong()
    {
        return ReadQWordUnsafe(stream);
    }

    public float LoadSingle()
    {
        var value = ReadDWordUnmanaged(stream);
        return Unsafe.As<uint, float>(ref value);
    }

    public double LoadDouble()
    {
        var value = ReadQWordUnsafe(stream);
        return Unsafe.As<ulong, double>(ref value);
    }

    public string LoadString()
    {
        var length = LoadInt();
        using var strBytes = BytesCluster.Rent(length);
        stream.ReadExactly(strBytes.Writer);
        return Encoding.UTF8.GetString(strBytes.Reader);
    }

    public (int length, char[] buffer) LoadStringToBuffer()
    {
        var strLen = LoadInt();
        using var strBytes = BytesCluster.Rent(strLen);
        stream.ReadExactly(strBytes.Writer);
        var charCount = Encoding.UTF8.GetCharCount(strBytes.Reader);
        var buffer = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            charCount = Encoding.UTF8.GetChars(strBytes.Reader, buffer.AsSpan()[..charCount]);
            return (charCount, buffer);
        }
        catch
        {
            ArrayPool<char>.Shared.Return(buffer);
            throw;
        }
    }

    public byte[] LoadBytes()
    {
        var length = LoadLong();
        var array = new byte[length];
        stream.ReadExactly(array);
        return array;
    }

    public (int length, byte[] buffer) LoadBytesToBuffer()
    {
        var length = (int)LoadLong();
        var array = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            stream.ReadExactly(array.AsSpan()[..length]);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(array);
            throw;
        }
        return (length, array);
    }

    public void LoadBuffer(Span<byte> span)
    {
        stream.ReadExactly(span);
    }

    public BytesCluster LoadBytesCluster()
    {
        var length = LoadLong();
        var array = BytesCluster.Rent((int)length);
        try
        {
            stream.ReadExactly(array.Writer);
            return array;
        }
        catch (Exception)
        {
            array.Dispose();
            throw;
        }
    }
}