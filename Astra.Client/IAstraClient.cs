using Astra.Engine;

namespace Astra.Client;

/// <summary>
/// Represents a client for interacting with Astra server.
/// </summary>
public interface IAstraClient : IDisposable
{
    /// <summary>
    /// Gets the connection settings used by this Astra client.
    /// </summary>
    public AstraConnectionSettings? ConnectionSettings { get; }
    
    /// <summary>
    /// Gets a value indicating whether the client is currently connected to the Astra server.
    /// </summary>
    public bool IsConnected { get; }
    
    /// <summary>
    /// Asynchronously connects to the Astra server using the specified connection settings.
    /// </summary>
    /// <param name="settings">The connection settings to use for the connection.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ConnectAsync(AstraConnectionSettings settings, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Asynchronously inserts a serializable object into the Astra database.
    /// </summary>
    /// <typeparam name="T">The type of the serializable object implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="value">The object to insert into the database.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of inserted rows.</returns>
    public Task<int> InsertSerializableAsync<T>(T value, CancellationToken cancellationToken = default) where T : IAstraSerializable;
    
    /// <summary>
    /// Asynchronously inserts a collection of serializable objects into the Astra database.
    /// </summary>
    /// <typeparam name="T">The type of the serializable objects implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="values">The collection of objects to insert into the database.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of inserted rows.</returns>
    public Task<int> BulkInsertSerializableAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : IAstraSerializable;

    /// <summary>
    /// Asynchronously aggregate data from Astra database and cast them to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the deserializable objects implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="predicateStream">The pre-serialized predicate used for aggregation.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the coroutine that will deserialize the retrieved data.</returns>
    public Task<IEnumerable<T>> SimpleAggregateAsync<T>(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default) where T : IAstraSerializable;

    /// <summary>
    /// Asynchronously count all rows from Astra database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the total number of rows.</returns>
    public Task<int> CountAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Asynchronously count all rows from Astra database that satisfy the condition provided by <paramref name="predicateStream"/>.
    /// </summary>
    /// /// <param name="predicateStream">The pre-serialized predicate used for aggregation.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the total number of satisfied rows.</returns>
    public Task<int> SimpleConditionalCountAsync(ReadOnlyMemory<byte> predicateStream, CancellationToken cancellationToken = default);
}