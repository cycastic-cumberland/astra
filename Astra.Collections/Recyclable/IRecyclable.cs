namespace Astra.Collections.Recyclable;

public interface IRecyclable
{
    public void Reset();
}

public interface IRecyclableFactory<out T> where T : IRecyclable
{
    public T Create();
}

public static class ClosureFactory
{
    public static ClosureFactory<T> ToFactory<T>(this Func<T> closure) where T : IRecyclable
    {
        return new(closure);
    }
}

public readonly struct ClosureFactory<T>(Func<T> closure) : IRecyclableFactory<T>
    where T : IRecyclable
{
    public T Create() => closure();
}