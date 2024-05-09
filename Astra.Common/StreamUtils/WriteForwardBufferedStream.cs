using System.Buffers;

namespace Astra.Common.StreamUtils;

public class WriteForwardBufferedStream : Stream
{
    private readonly byte[] _buffer;
    private int _index;

    private int RemainingVacant => _buffer.Length - _index;
    
    private Span<byte> Buffer => new(_buffer, 0, _index);
    
    private readonly Stream _targetStream;

    public WriteForwardBufferedStream(Stream targetStream, int bufferSize = 1024)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        _targetStream = targetStream;
    }
    public override void Flush()
    {
        _targetStream.Write(Buffer);
        _index = 0;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Close()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var remaining = buffer.Length;
        while (remaining > 0)
        {
            int chunkSize;
            if (remaining > RemainingVacant)
            {
                chunkSize = RemainingVacant;
                buffer[..chunkSize].CopyTo(new(_buffer, _index, chunkSize));
                Flush();
            }
            else
            {
                chunkSize = remaining;
                buffer.CopyTo(new(_buffer, _index, chunkSize));
            }
            _index += chunkSize;
            remaining -= chunkSize;
            buffer = buffer[chunkSize..];
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(new(buffer, offset, count));
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException(); 
        set => throw new NotSupportedException();
    }
}