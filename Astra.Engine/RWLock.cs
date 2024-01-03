namespace Astra.Engine;

public readonly struct RWLock(int timeout)
{
    private readonly ReaderWriterLock _lock = new();

    public static RWLock Create(int timeout = int.MaxValue) => new(timeout);
    
    public readonly struct ReadLockInstance : IDisposable
    {
        private readonly ReaderWriterLock _rwLock;

        public ReadLockInstance(ReaderWriterLock rwLock, int timeout)
        {
            _rwLock = rwLock;
            _rwLock.AcquireReaderLock(timeout);
        }

        public void Dispose()
        {
            _rwLock.ReleaseReaderLock();
        }
    }
    public readonly struct WriteLockInstance : IDisposable
    {
        private readonly ReaderWriterLock _rwLock;

        public WriteLockInstance(ReaderWriterLock rwLock, int timeout)
        {
            _rwLock = rwLock;
            _rwLock.AcquireWriterLock(timeout);
        }

        public void Dispose()
        {
            _rwLock.ReleaseWriterLock();
        }
    }

    public ReadLockInstance Read() => new(_lock, timeout);
    public WriteLockInstance Write() => new(_lock, timeout);

    public T Read<T>(Func<T> func)
    {
        using var guard = Read();
        return func();
    }
    public T Write<T>(Func<T> func)
    {
        using var guard = Write();
        return func();
    }
}