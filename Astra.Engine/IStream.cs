namespace Astra.Engine;

public interface IStream : IReadOnlyStream
{
    public void Flush();

    public void WriteByte(byte value);
    public void Write(byte[] bytes, int offset, int count);
    public void Write(ReadOnlySpan<byte> bytes);

    public ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);
}
