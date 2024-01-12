namespace Astra.Common;

public interface IStreamWrapper
{
    public void SaveValue(int value);
    public ValueTask SaveValueAsync(int value, CancellationToken cancellationToken = default);
    public void SaveValue(uint value);
    public ValueTask SaveValueAsync(uint value, CancellationToken cancellationToken = default);
    public void SaveValue(long value);
    public ValueTask SaveValueAsync(long value, CancellationToken cancellationToken = default);
    public void SaveValue(ulong value);
    public ValueTask SaveValueAsync(ulong value, CancellationToken cancellationToken = default);
    public void SaveValue(string value);
    public ValueTask SaveValueAsync(string value, CancellationToken cancellationToken = default);
    public void SaveValue(byte[] value);
    public ValueTask SaveValueAsync(byte[] value, CancellationToken cancellationToken = default);
    public void SaveValue(BytesCluster value);
    public ValueTask SaveValueAsync(BytesCluster value, CancellationToken cancellationToken = default);
    public int LoadInt();
    public uint LoadUInt();
    public long LoadLong();
    public ulong LoadULong();
    public string LoadString();
    public byte[] LoadBytes();
    public BytesCluster LoadBytesCluster();
}
