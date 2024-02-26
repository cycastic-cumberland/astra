using System.Collections.Concurrent;

namespace Astra.Collections.Recyclable;

public class ConcurrentObjectPool<T, TFactory>(TFactory factory) : IObjectPool<T> 
    where T : IRecyclable
    where TFactory : IRecyclableFactory<T>
{
    private readonly ConcurrentBag<T> _bag = new();

    public void Return(T subject)
    {
        subject.Reset();
        _bag.Add(subject);
    }

    public T Retrieve()
    {
        return !_bag.TryTake(out var existing) ? factory.Create() : existing;
    }
}
