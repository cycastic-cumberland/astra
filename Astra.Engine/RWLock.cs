using System.Runtime.CompilerServices;

namespace Astra.Engine;


// Some how ReaderWriterLockSlim does not work with benchmark
public readonly struct RWLock(ReaderWriterLockSlim rwLock)
{
    public static RWLock Create() => new(new());
    
    public readonly struct ReadLockInstance : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        public ReadLockInstance(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock.EnterReadLock();
        }

        public void Dispose()
        {
            _rwLock.ExitReadLock();
        }
    }
    public readonly struct WriteLockInstance : IDisposable
    {
        private readonly ReaderWriterLockSlim _rwLock;

        
        public WriteLockInstance(ReaderWriterLockSlim rwLock)
        {
            _rwLock = rwLock;
            _rwLock.EnterWriteLock();
        }

        public void Dispose()
        {
            _rwLock.ExitWriteLock();
        }
    }

    public ReadLockInstance Read() => new(rwLock);
    public WriteLockInstance Write() => new(rwLock);

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