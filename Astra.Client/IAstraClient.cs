namespace Astra.Client;

public interface IAstraClient : IDisposable
{
    public AstraConnectionSettings ConnectionSettings { get; }
    public bool IsConnected { get; }
    public Task<int> UnorderedInsertAsync(MemoryStream dataStream, bool autoDisposeStream = false, CancellationToken cancellationToken = default);
}