using System.Runtime.CompilerServices;

namespace Astra.Common;

public readonly struct ForwardStreamWrapper(Stream stream) : IStreamWrapper
{
    public void SaveValue(int value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(int value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    public void SaveValue(uint value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(uint value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    public void SaveValue(long value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(long value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    public void SaveValue(ulong value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    public void SaveValue(string value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(string value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    public void SaveValue(byte[] value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(value.AsMemory(), cancellationToken);
    }

    public void SaveValue(BytesCluster value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(BytesCluster value, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(value.ReaderMemory, cancellationToken);
    }

    public int LoadInt()
    {
        return stream.ReadInt();
    }

    public uint LoadUInt()
    {
        return stream.ReadUInt();
    }

    public long LoadLong()
    {
        return stream.ReadLong();
    }

    public ulong LoadULong()
    {
        return stream.ReadULong();
    }

    public string LoadString()
    {
        return stream.ReadString();
    }

    public byte[] LoadBytes()
    {
        return stream.ReadSequence();
    }

    public BytesCluster LoadBytesCluster()
    {
        return stream.ReadCluster();
    }
}