using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Protocols;

namespace Astra.Common.StreamUtils;

public static class StreamWriteAddons
{
    public static void WriteValue(this Stream writer, bool value)
    {
        Span<byte> buffer = stackalloc byte[1] { unchecked((byte)(value ? 1 : 0)) };
        writer.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnmanagedValue<T>(this Stream writer, T value) where T : unmanaged
    {
        writer.Write(MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), Unsafe.SizeOf<T>()));
    }

    public static void WriteWildcard(this Stream writer, object? obj)
    {
        if (obj == null) throw new ArgumentException(nameof(obj));
        var type = obj.GetType();
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int32:
                writer.WriteUnmanagedValue((int)obj);
                return;
            case TypeCode.Int64:
                writer.WriteUnmanagedValue((long)obj);
                return;
            case TypeCode.Single:
                writer.WriteUnmanagedValue((float)obj);
                return;
            case TypeCode.Double:
                writer.WriteUnmanagedValue((double)obj);
                return;
            case TypeCode.Decimal:
                writer.WriteUnmanagedValue((decimal)obj);
                return;
            default:
                if (type == typeof(string))
                {
                    writer.WriteValue((string)obj);
                    return;
                }

                if (type == typeof(byte[]))
                {
                    writer.WriteValue((byte[])obj);
                    return;
                }
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteValue(this Stream writer, byte value)
    {
        writer.WriteByte(value);
    }
    
    public static void WriteValue(this Stream writer, int value)
    {
        writer.WriteUnmanagedValue(value);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, int value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, uint value)
    {
        writer.WriteUnmanagedValue(value);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, uint value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, float value)
    {
        writer.WriteUnmanagedValue(value);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, float value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, double value)
    {
        writer.WriteUnmanagedValue(value);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, double value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, long value)
    {
        writer.WriteUnmanagedValue(value);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, long value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static ValueTask WriteValueAsync(this Stream writer, ulong value, CancellationToken token = default)
    {
        return writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, ulong value)
    {
        writer.WriteUnmanagedValue(value);
    }

    private static void WriteShortString(this Stream writer, ReadOnlySpan<char> value)
    {
        // Reference: https://www.rfc-editor.org/rfc/rfc3629 (Section 3. UTF-8 definition)
        // TLDR: The max number of bytes per UTF-8 character is 4 
        Span<byte> bytes = stackalloc byte[value.Length * 4];
        var written = Encoding.UTF8.GetBytes(value, bytes);
        writer.WriteValue(written);
        writer.Write(bytes[..written]);
    }
    
    private static void WriteLongString(this Stream writer, ReadOnlySpan<char> value)
    {
        using var buffer = BytesCluster.Rent(value.Length * 4);
        var bytes = buffer.Writer;
        var written = Encoding.UTF8.GetBytes(value, bytes);
        writer.WriteValue(written);
        writer.Write(bytes[..written]);
    }
    
    public static void WriteValue(this Stream writer, string? value)
    {
        if (value == null) throw new ArgumentException(nameof(value));
        if (value.Length < CommonProtocol.LongStringThreshold) writer.WriteShortString(value);
        else writer.WriteLongString(value);
    }
    
    public static void WriteValue(this Stream writer, StringRef value)
    {
        if (value.Length < CommonProtocol.LongStringThreshold) writer.WriteShortString(value);
        else writer.WriteLongString(value);
    }
    
    public static async ValueTask WriteValueAsync(this Stream writer, string value, CancellationToken token = default)
    {
        if (value == null!) throw new ArgumentException(nameof(value));
        var strArr = Encoding.UTF8.GetBytes(value);
        await writer.WriteValueAsync(strArr.Length, token: token);
        await writer.WriteAsync(strArr, token);
    }

    public static void WriteValue(this Stream writer, DateTime value)
    {
        var kind = value.Kind;
        var epoch = DateTime.MinValue.ToUniversalTime();
        var span = value.ToUniversalTime() - epoch;
        writer.WriteValue(kind == DateTimeKind.Local);
        writer.WriteValue(span.Ticks);
    }
    public static void WriteValue(this Stream writer, BytesCluster array)
    {
        writer.WriteValue(array.LongLength);
        writer.Write(array.Reader);
    }

    public static void WriteValue(this Stream writer, byte[] array)
    {
        writer.WriteValue(array.LongLength);
        writer.Write(array);
    }
    
    public static void WriteValue(this Stream writer, ReadOnlySpan<byte> array)
    {
        writer.WriteValue((long)array.Length);
        writer.Write(array);
    }

    public static void WriteValue(this Stream writer, ReadOnlyMemory<byte> array)
        => writer.WriteValue(array.Span);

    public static async ValueTask WriteValueAsync(this Stream writer, byte[] value, CancellationToken token = default)
    {
        await writer.WriteValueAsync(value.LongLength, token);
        await writer.WriteAsync(value, token);
    }
    
    public static async ValueTask WriteValueAsync(this Stream writer, BytesCluster value, CancellationToken token = default)
    {
        await writer.WriteValueAsync(value.LongLength, token);
        await writer.WriteAsync(value.ReaderMemory, token);
    }

    public static async ValueTask WriteValueAsync(this Stream writer, Hash256 value, CancellationToken token = default)
    {
        using var buffer = BytesCluster.Rent(Hash256.Size);
        value.CopyTo(buffer.Writer);
        await writer.WriteAsync(buffer.ReaderMemory, token);
    }

    public static void WriteValue(this Stream writer, Hash128 hash)
    {
        writer.WriteUnmanagedValue(hash);
    }
}