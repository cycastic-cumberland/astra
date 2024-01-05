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
    /// Asynchronously inserts a value type implementing <see cref="IAstraSerializable"/> into the Astra database.
    /// </summary>
    /// <typeparam name="T">The value type implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="value">The value to insert into the database.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of inserted rows.</returns>
    public Task<int> InsertSerializableValueAsync<T>(T value, CancellationToken cancellationToken = default) where T : struct, IAstraSerializable;
    
    /// <summary>
    /// Asynchronously inserts a collection of serializable objects into the Astra database.
    /// </summary>
    /// <typeparam name="T">The type of the serializable objects implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="values">The collection of objects to insert into the database.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of inserted rows.</returns>
    public Task<int> InsertSerializableAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : IAstraSerializable;
    
    /// <summary>
    /// Asynchronously inserts a collection of value types implementing <see cref="IAstraSerializable"/> into the Astra database.
    /// </summary>
    /// <typeparam name="T">The value type implementing <see cref="IAstraSerializable"/>.</typeparam>
    /// <param name="values">The collection of values to insert into the database.</param>
    /// <param name="cancellationToken">The cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of inserted rows.</returns>
    public Task<int> InsertSerializableValueAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default) where T : struct, IAstraSerializable;
}