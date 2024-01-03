using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Astra.Engine;

public static class StreamReadAddons
{
    public static bool ReadBoolean(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(bool)];
        reader.ReadExactly(arr);
        return arr[0] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(int)];
        reader.ReadExactly(arr);
        return BitConverter.ToInt32(arr);
    }
    
    public static uint ReadUInt(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(uint)];
        reader.ReadExactly(arr);
        return BitConverter.ToUInt32(arr);
    }
    
    public static double ReadDouble(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(double)];
        reader.ReadExactly(arr);
        return BitConverter.ToDouble(arr);
    }
    
    public static long ReadLong(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(long)];
        reader.ReadExactly(arr);
        return BitConverter.ToInt64(arr);
    }
    
    public static async Task<long> ReadLongAsync(this Stream reader, CancellationToken token = default)
    {
        var bytes = new byte[sizeof(long)];
        await reader.ReadExactlyAsync(bytes, token);
        return BitConverter.ToInt64(bytes);
    }
    
    public static ulong ReadULong(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(ulong)];
        reader.ReadExactly(arr);
        return BitConverter.ToUInt64(arr);
    }

    public static string ReadString(this Stream reader)
    {
        var strLen = ReadInt(reader);
        Span<byte> strArr = stackalloc byte[strLen];
        reader.ReadExactly(strArr);
        var str = Encoding.UTF8.GetString(strArr);
        return str;
    }

    public static DateTime ReadDateTime(this Stream reader)
    {
        var kind = reader.ReadBoolean() ? DateTimeKind.Local : DateTimeKind.Utc;
        var epoch = DateTime.MinValue;
        if (kind == DateTimeKind.Utc) epoch = epoch.ToUniversalTime();
        var ticks = reader.ReadLong();
        var span = TimeSpan.FromTicks(ticks);
        var dt = epoch + span;
        return dt;
    }

    public static T ReadGeneric<T>(this Stream reader) where T : struct
    {
        var ret = new T();
        var size = Marshal.SizeOf(ret);
        Span<byte> bytesArr = stackalloc byte[size];
        reader.ReadExactly(bytesArr);
        ret = MemoryMarshal.Read<T>(bytesArr);

        return ret;
    }
    
    public static T? ReadNullable<T>(this Stream reader) where T : struct
    {
        var notNull = reader.ReadBoolean();
        if (!notNull) return null;
        return ReadGeneric<T>(reader);
    }

    public static byte[] ReadSequence(this Stream reader)
    {
        var length = reader.ReadLong();
        var array = new byte[length];
        reader.ReadExactly(array);
        return array;
    }
    
    public static BytesCluster ReadCluster(this Stream reader)
    {
        var length = reader.ReadLong();
        var array = BytesCluster.Rent((int)length);
        try
        {
            reader.ReadExactly(array.Writer);
            return array;
        }
        catch (Exception)
        {
            array.Dispose();
            throw;
        }
    }
    
    public static async Task<BytesCluster> ReadClusterAsync(this Stream reader)
    {
        var length = await reader.ReadLongAsync();
        var array = BytesCluster.Rent((int)length);
        try
        {
            await reader.ReadExactlyAsync(array.WriterMemory);
            return array;
        }
        catch (Exception)
        {
            array.Dispose();
            throw;
        }
    }
}