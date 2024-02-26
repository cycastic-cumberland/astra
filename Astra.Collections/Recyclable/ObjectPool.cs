namespace Astra.Collections.Recyclable;

public class ObjectPool<T, TFactory>(TFactory factory, bool doLateReset = true) : IObjectPool<T> 
    where T : IRecyclable
    where TFactory : IRecyclableFactory<T>
{
    private readonly Stack<T> _bag = new();
    public void Return(T subject)
    {
        if (!doLateReset)
            subject.Reset();
        _bag.Push(subject);
    }

    public T Retrieve()
    {
        if (!_bag.TryPop(out var existing)) return factory.Create();
        if (doLateReset)
            existing.Reset();
        return existing;
    }
}