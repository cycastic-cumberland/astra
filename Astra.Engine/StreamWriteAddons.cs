using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Astra.Engine;

public static class StreamWriteAddons
{
    public static void WriteValue(this Stream writer, bool value)
    {
        Span<byte> buffer = stackalloc byte[1] { unchecked((byte)(value ? 1 : 0)) };
        writer.Write(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteValueInternal(this Stream writer, void* ptr, int size)
    {
        writer.Write(new ReadOnlySpan<byte>(ptr, size));
    }
    
    public static void WriteValue(this Stream writer, int value)
    {
        // writer.Write(BitConverter.GetBytes(value));
        unsafe
        {
            writer.WriteValueInternal(&value, sizeof(int));
        }
    }
    
    public static async Task WriteValueAsync(this Stream writer, int value, CancellationToken token = default)
    {
        await writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, uint value)
    {
        // writer.Write(BitConverter.GetBytes(value));
        unsafe
        {
            writer.WriteValueInternal(&value, sizeof(uint));
        }
    }
    
    public static async Task WriteValueAsync(this Stream writer, uint value, CancellationToken token = default)
    {
        await writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, double value)
    {
        // writer.Write(BitConverter.GetBytes(value));
        unsafe
        {
            writer.WriteValueInternal(&value, sizeof(double));
        }
    }
    
    public static void WriteValue(this Stream writer, long value)
    {
        // writer.Write(BitConverter.GetBytes(value));
        unsafe
        {
            writer.WriteValueInternal(&value, sizeof(long));
        }
    }
    
    public static async Task WriteValueAsync(this Stream writer, long value, CancellationToken token = default)
    {
        await writer.WriteAsync(BitConverter.GetBytes(value), token);
    }
    
    public static void WriteValue(this Stream writer, ulong value)
    {
        unsafe
        {
            writer.WriteValueInternal(&value, sizeof(ulong));
        }
    }

    public static void WriteValue(this Stream writer, string value)
    {
        if (value == null!) throw new ArgumentException(nameof(value));
        var strArr = Encoding.UTF8.GetBytes(value);
        writer.WriteValue(strArr.Length);
        writer.Write(strArr);
    }
    
    public static async Task WriteValueAsync(this Stream writer, string value, CancellationToken token = default)
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
    
    public static void WriteValue<T>(this Stream writer, T value) where T : struct
    {
        var size = Marshal.SizeOf(value);
        unsafe
        {
            var ptr = stackalloc byte[size];
            var arr = new Span<byte>(ptr, size);
            Marshal.StructureToPtr(value, (nint)ptr, true);
            writer.Write(arr);
        }
    }
    
    public static void WriteValue<T>(this Stream writer, T? value) where T : struct
    {
        if (value != null)
        {
            writer.WriteValue(true);
            WriteValue(writer, value.Value);
        }
        else
        {
            writer.WriteValue(false);
        }
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

    public static void WriteValue(this Stream writer, Hash128 hash)
    {
        unsafe
        {
            writer.WriteValueInternal(&hash, Hash128.Size);
        }
    }
}