namespace Astra.Client;

public interface IAstraClient : IDisposable
{
    public AstraConnectionSettings? ConnectionSettings { get; }
    public bool IsConnected { get; }
    public Task ConnectAsync(AstraConnectionSettings settings, CancellationToken cancellationToken = default);
    public Task<int> UnorderedInsertClassType<T>(T value, CancellationToken cancellationToken = default) where T : class, IAstraSerializable;
    public Task<int> UnorderedInsertValueType<T>(T value, CancellationToken cancellationToken = default) where T : struct, IAstraSerializable;
    public Task<int> UnorderedBulkInsertClassType<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : class, IAstraSerializable;
    public Task<int> UnorderedBulkInsertValueType<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : struct, IAstraSerializable;
}