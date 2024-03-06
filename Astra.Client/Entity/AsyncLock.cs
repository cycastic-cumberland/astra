namespace Astra.Client.Entity;

public readonly struct AsyncLock : IDisposable
{
    private readonly object _obj;
    
    public AsyncLock(object? obj)
    {
        _obj = obj ?? throw new ArgumentException(nameof(obj));
        Monitor.Enter(_obj);
    }

    public void Dispose()
    {
        Monitor.Exit(_obj);
    }

    public static AsyncLock Create(object? obj) => new(obj);
}

public static class AsyncLockHelper
{
    public static AsyncLock LockAsync(this object obj) => new(obj);
}