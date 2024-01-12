using System.Runtime.CompilerServices;
using System.Text;

namespace Astra.Common;

public readonly struct ReverseStreamWrapper(Stream stream) : IStreamWrapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReverseEndianness(uint value)
    {
        return ((value >> 24) & 0xFF) |
                ((value >> 8) & 0xFF00) |
                ((value << 8) & 0xFF0000) |
                ((value << 24) & 0xFF000000);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteDWord(Stream writer, void* ptr)
    {
        const int size = sizeof(uint);
        var bytes = ReverseEndianness(*(uint*)ptr);
        writer.Write(new ReadOnlySpan<byte>(&bytes, size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint ReadDWord(Stream reader)
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
                ((value << 56) & 0xFF00000000000000);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteQWord(Stream writer, void* ptr)
    {
        const int size = sizeof(ulong);
        var bytes = ReverseEndianness(*(ulong*)ptr);
        writer.Write(new ReadOnlySpan<byte>(&bytes, size));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong ReadQWord(Stream reader)
    {
        const int size = sizeof(ulong);
        ulong bytes;
        reader.ReadExactly(new Span<byte>(&bytes, size));
        return ReverseEndianness(bytes);
    }
    public void SaveValue(int value)
    {
        unsafe
        {
            WriteDWord(stream, &value);
        }
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
        unsafe
        {
            WriteDWord(stream, &value);
        }
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
        unsafe
        {
            WriteQWord(stream, &value);
        }
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
        unsafe
        {
            WriteQWord(stream, &value);
        }
    }

    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default)
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
        unsafe
        {
            WriteDWord(stream, &written);
        }
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
        return unchecked((int)ReadDWord(stream));
    }

    public uint LoadUInt()
    {
        return ReadDWord(stream);
    }

    public long LoadLong()
    {
        return unchecked((long)ReadQWord(stream));
    }

    public ulong LoadULong()
    {
        return ReadQWord(stream);
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