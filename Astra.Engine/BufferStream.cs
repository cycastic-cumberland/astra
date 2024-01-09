using System.Runtime.CompilerServices;

namespace Astra.Engine;

public class ReadOnlyBufferStream : Stream, IReadOnlyStream
{
    private long _pos;

    private ReadOnlyMemory<byte> _targetBuffer;

    public ReadOnlyMemory<byte> Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _targetBuffer;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _targetBuffer = value;
            _pos = 0;
        }
    }

    public ReadOnlyBufferStream()
    {
        _targetBuffer = new();
    }
    
    public ReadOnlyBufferStream(ReadOnlyMemory<byte> targetBuffer)
    {
        _targetBuffer = targetBuffer;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int ReadByte()
    {
        Span<byte> bytes = stackalloc byte[1];
        ReadExactly(bytes);
        return bytes[0];
    }

    private int ReadUnchecked(Span<byte> buffer, int endOffset)
    { 
        _targetBuffer.Span[(int)_pos..endOffset].CopyTo(buffer);
        var oldPos = _pos;
        _pos = endOffset;
        return (int)(endOffset - oldPos);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadChecked(Span<byte> buffer)
    {
        var endOffset = (int)(buffer.Length + _pos);
        endOffset = endOffset >= _targetBuffer.Length ? _targetBuffer.Length : endOffset;
        return ReadUnchecked(buffer, endOffset);
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadChecked(new(buffer, offset, count));
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPos = offset;
        switch (origin)
        {
            case SeekOrigin.Begin:
                break;
            case SeekOrigin.Current:
                newPos += _pos;
                break;
            case SeekOrigin.End:
                newPos = _targetBuffer.Length - newPos;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        if (newPos < 0 || newPos >= _targetBuffer.Length) throw new IndexOutOfRangeException();
        _pos = newPos;
        return _pos;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }


    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _targetBuffer.Length;
    public override long Position 
    {
        get => _pos;
        set => Seek(value, SeekOrigin.Begin);
    }
}
