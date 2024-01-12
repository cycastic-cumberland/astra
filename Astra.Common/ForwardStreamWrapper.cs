using System.Runtime.CompilerServices;

namespace Astra.Common;

public readonly struct ForwardStreamWrapper(Stream stream) : IStreamWrapper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(int value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(int value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(uint value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(uint value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(long value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(long value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(ulong value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(string value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(string value, CancellationToken cancellationToken = default)
    {
        return stream.WriteValueAsync(value, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(byte[] value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(byte[] value, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(value.AsMemory(), cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SaveValue(BytesCluster value)
    {
        stream.WriteValue(value);
    }

    public ValueTask SaveValueAsync(BytesCluster value, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(value.ReaderMemory, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LoadInt()
    {
        return stream.ReadInt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint LoadUInt()
    {
        return stream.ReadUInt();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long LoadLong()
    {
        return stream.ReadLong();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong LoadULong()
    {
        return stream.ReadULong();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string LoadString()
    {
        return stream.ReadString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] LoadBytes()
    {
        return stream.ReadSequence();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BytesCluster LoadBytesCluster()
    {
        return stream.ReadCluster();
    }
}