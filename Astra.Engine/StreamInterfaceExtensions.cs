using System.Runtime.CompilerServices;
using Astra.Common;

namespace Astra.Engine;

public static class StreamInterfaceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StreamWrapper<TStream> Wrap<TStream>(this TStream stream) where TStream : Stream
    {
        return new(stream);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StreamWrapper Wrap(this Stream stream)
    {
        return new(stream);
    }
}

public readonly struct StreamWrapper(Stream stream) : IStream
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        stream.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush()
    {
        stream.Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close()
    {
        stream.Close();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadByte()
    {
        return stream.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.ReadAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadExactly(Span<byte> span)
    {
        stream.ReadExactly(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask ReadExactlyAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.ReadExactlyAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Seek(long offset, SeekOrigin origin)
    {
        return stream.Seek(offset, origin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLength(long value)
    {
        stream.SetLength(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte[] bytes, int offset, int count)
    {
        stream.Write(bytes, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> bytes)
    {
        stream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        stream.WriteByte(value);
    }

    public bool CanRead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public bool CanSeek 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public bool CanWrite 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public long Length 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Length;
    }

    public long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => stream.Position = value;
    }
}

public readonly struct StreamWrapper<TStream>(TStream stream) : IStream where TStream : Stream
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        stream.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush()
    {
        stream.Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close()
    {
        stream.Close();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadByte()
    {
        return stream.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.ReadAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadExactly(Span<byte> span)
    {
        stream.ReadExactly(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask ReadExactlyAsync(Memory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.ReadExactlyAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Seek(long offset, SeekOrigin origin)
    {
        return stream.Seek(offset, origin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLength(long value)
    {
        stream.SetLength(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte[] bytes, int offset, int count)
    {
        stream.Write(bytes, offset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(bytes, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> bytes)
    {
        stream.Write(bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        stream.WriteByte(value);
    }

    public bool CanRead
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public bool CanSeek 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public bool CanWrite 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.CanRead;
    }
    public long Length 
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Length;
    }

    public long Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stream.Position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => stream.Position = value;
    }
}