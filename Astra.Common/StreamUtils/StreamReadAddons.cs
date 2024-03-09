using System.Runtime.CompilerServices;
using System.Text;
using Astra.Common.Data;

namespace Astra.Common.StreamUtils;

public static class StreamReadAddons
{
    public static bool ReadBoolean(this Stream reader)
    {
        Span<byte> arr = stackalloc byte[sizeof(bool)];
        reader.ReadExactly(arr);
        return arr[0] != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T ReadUnmanagedStruct<T>(this Stream reader) where T : unmanaged
    {
        void* ptr = stackalloc byte[sizeof(T)];
        reader.ReadExactly(new Span<byte>(ptr, sizeof(T)));
        // Type punning magic
        return *(T*)ptr;
    }
    
    public static int ReadInt(this Stream reader)
    {
        return ReadUnmanagedStruct<int>(reader);
    }
    
    public static async ValueTask<int> ReadIntAsync(this Stream reader, CancellationToken token = default)
    {
        var bytes = new byte[sizeof(int)];
        await reader.ReadExactlyAsync(bytes, token);
        return BitConverter.ToInt32(bytes);
    }
    
    public static uint ReadUInt(this Stream reader)
    {
        return ReadUnmanagedStruct<uint>(reader);
    }
    
    public static float ReadSingle(this Stream reader)
    {
        return ReadUnmanagedStruct<float>(reader);
    }
    
    public static double ReadDouble(this Stream reader)
    {
        return ReadUnmanagedStruct<double>(reader);
    }
    
    public static decimal ReadDecimal(this Stream reader)
    {
        return ReadUnmanagedStruct<decimal>(reader);
    }
    
    public static long ReadLong(this Stream reader)
    {
        return ReadUnmanagedStruct<long>(reader);
    }
    
    public static async ValueTask<long> ReadLongAsync(this Stream reader, CancellationToken token = default)
    {
        var bytes = new byte[sizeof(long)];
        await reader.ReadExactlyAsync(bytes, token);
        return BitConverter.ToInt64(bytes);
    }
    
    public static ulong ReadULong(this Stream reader)
    {
        return ReadUnmanagedStruct<ulong>(reader);
    }
    
    public static async ValueTask<ulong> ReadULongAsync(this Stream reader, CancellationToken token = default)
    {
        var bytes = new byte[sizeof(ulong)];
        await reader.ReadExactlyAsync(bytes, token);
        return BitConverter.ToUInt64(bytes);
    }

    public static string ReadString(this Stream reader)
    {
        var strLen = ReadInt(reader);
        using var strBytes = BytesCluster.Rent(strLen);
        reader.ReadExactly(strBytes.Writer);
        var str = Encoding.UTF8.GetString(strBytes.Reader);
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
    
    public static async ValueTask<BytesCluster> ReadClusterAsync(this Stream reader, CancellationToken cancellationToken = default)
    {
        var length = await reader.ReadLongAsync(token: cancellationToken);
        var array = BytesCluster.Rent((int)length);
        try
        {
            await reader.ReadExactlyAsync(array.WriterMemory, cancellationToken);
            return array;
        }
        catch (Exception)
        {
            array.Dispose();
            throw;
        }
    }
}