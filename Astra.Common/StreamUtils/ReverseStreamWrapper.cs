using System.Runtime.CompilerServices;
using System.Text;
using Astra.Common.Data;
using Astra.Common.Protocols;

namespace Astra.Common.StreamUtils;

public readonly struct ReverseStreamWrapper(Stream stream) : IStreamWrapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseEndianness(uint value)
    {
        return ((value >> 24) & 0xFF) |
                ((value >> 8) & 0xFF00) |
                ((value << 8) & 0xFF0000) |
                ((value << 24) & 0xFF000000U);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteDWordUnsafe<T>(Stream writer, T value) where T : unmanaged
    {
        const int size = sizeof(uint);
        var bytes = ReverseEndianness(*(uint*)&value);
        writer.Write(new ReadOnlySpan<byte>(&bytes, size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint ReadDWordUnsafe(Stream reader)
    {
        const int size = sizeof(uint);
        uint bytes;
        reader.ReadExactly(new Span<byte>(&bytes, size));
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
    private static unsafe void WriteQWordUnsafe<T>(Stream writer, T value) where T : unmanaged
    {
        const int size = sizeof(ulong);
        var bytes = ReverseEndianness(*(ulong*)&value);
        writer.Write(new ReadOnlySpan<byte>(&bytes, size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong ReadQWordUnsafe(Stream reader)
    {
        const int size = sizeof(ulong);
        ulong bytes;
        reader.ReadExactly(new Span<byte>(&bytes, size));
        return ReverseEndianness(bytes);
    }
    public void SaveValue(int value)
    {
        WriteDWordUnsafe(stream, value);
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
        WriteDWordUnsafe(stream, value);
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
        WriteDWordUnsafe(stream, value);
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

    private void WriteShortString(string value)
    {
        // Reference: https://www.rfc-editor.org/rfc/rfc3629 (Section 3. UTF-8 definition)
        // TLDR: The max number of bytes per UTF-8 character is 4 
        Span<byte> bytes = stackalloc byte[value.Length * 4];
        var written = Encoding.UTF8.GetBytes(value.AsSpan(), bytes);
        SaveValue(written);
        stream.Write(bytes[..written]);
    }
    
    private void WriteLongString(string value)
    {
        var strArr = Encoding.UTF8.GetBytes(value);
        SaveValue(strArr.Length);
        stream.Write(strArr);
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

    public void SaveValue(byte[] value)
    {
        SaveValue(value.LongLength);
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

    public int LoadInt()
    {
        return unchecked((int)ReadDWordUnsafe(stream));
    }

    public uint LoadUInt()
    {
        return ReadDWordUnsafe(stream);
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
        unsafe
        {
            var value = ReadDWordUnsafe(stream);
            return *(float*)&value;
        }
    }

    public double LoadDouble()
    {
        unsafe
        {
            var value = ReadQWordUnsafe(stream);
            return *(double*)&value;
        }
    }

    public string LoadString()
    {
        var length = LoadInt();
        using var strBytes = BytesCluster.Rent(length);
        stream.ReadExactly(strBytes.Writer);
        return Encoding.UTF8.GetString(strBytes.Reader);
    }

    public byte[] LoadBytes()
    {
        var length = LoadLong();
        var array = new byte[length];
        stream.ReadExactly(array);
        return array;
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