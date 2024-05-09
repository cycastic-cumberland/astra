using System.Net.Sockets;
using System.Text;
using Astra.Common.Data;

namespace Astra.Common.StreamUtils;

public readonly struct NetworkStreamWrapper<T>(T stream, TcpClient client, int timeout) : IStreamWrapper where T : IStreamWrapper
{
    public void SaveValue(byte value)
    {
        stream.SaveValue(value);
    }

    public void SaveValue(int value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(int value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(uint value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(uint value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(long value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(long value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(ulong value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(float value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(float value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(double value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(double value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(string value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(string value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(StringRef value)
    {
        stream.SaveValue(value);
    }

    public void SaveValue(byte[] value)
    {
        stream.SaveValue(value);
    }

    public void SaveValue(ReadOnlySpan<byte> value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public void SaveValue(BytesCluster value)
    {
        stream.SaveValue(value);
    }

    public ValueTask SaveValueAsync(BytesCluster value, CancellationToken cancellationToken = default)
    {
        return stream.SaveValueAsync(value, cancellationToken);
    }

    public byte LoadByte()
    {
        return stream.LoadByte();
    }

    public int LoadInt()
    {
        client.NetworkSpinLock(sizeof(int), timeout);
        return stream.LoadInt();
    }

    public uint LoadUInt()
    {
        client.NetworkSpinLock(sizeof(uint), timeout);
        return stream.LoadUInt();
    }

    public long LoadLong()
    {
        client.NetworkSpinLock(sizeof(long), timeout);
        return stream.LoadLong();
    }

    public ulong LoadULong()
    {
        client.NetworkSpinLock(sizeof(ulong), timeout);
        return stream.LoadULong();
    }

    public float LoadSingle()
    {
        client.NetworkSpinLock(sizeof(float), timeout);
        return stream.LoadSingle();
    }

    public double LoadDouble()
    {
        client.NetworkSpinLock(sizeof(double), timeout);
        return stream.LoadDouble();
    }

    public string LoadString()
    {
        client.NetworkSpinLock(sizeof(int), timeout);
        var length = stream.LoadInt();
        using var strBytes = BytesCluster.Rent(length);
        LoadBuffer(strBytes.Writer);
        return Encoding.UTF8.GetString(strBytes.Reader);
    }

    public byte[] LoadBytes()
    {
        client.NetworkSpinLock(sizeof(long), timeout);
        var length = stream.LoadLong();
        var buffer = new byte[length];
        LoadBuffer(buffer);
        return buffer;
    }

    public void LoadBuffer(Span<byte> span)
    {
        client.NetworkWait(span.Length, timeout);
        stream.LoadBuffer(span);
    }

    public BytesCluster LoadBytesCluster()
    {
        client.NetworkSpinLock(sizeof(long), timeout);
        var length = stream.LoadLong();
        var cluster = BytesCluster.Rent((uint)length);
        try
        {
            LoadBuffer(cluster.Writer);
            return cluster;
        }
        catch
        {
            cluster.Dispose();
            throw;
        }
    }
}