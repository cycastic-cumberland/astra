namespace Astra.Engine;

public interface IReadOnlyStream : IDisposable
{
    public void Close();

    public int ReadByte();
    public int Read(byte[] buffer, int offset, int count);
    public ValueTask<int> ReadAsync(Memory<byte> bytes, CancellationToken cancellationToken = default);
    public void ReadExactly(Span<byte> span);
    public ValueTask ReadExactlyAsync(Memory<byte> bytes, CancellationToken cancellationToken = default);
    public long Seek(long offset, SeekOrigin origin);
    public void SetLength(long value);
    public bool CanRead { get; }
    public bool CanSeek { get; }
    public bool CanWrite { get; }
    public long Length { get; }
    public long Position { get; set; }
}