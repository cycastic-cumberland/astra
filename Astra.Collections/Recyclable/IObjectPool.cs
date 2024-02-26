namespace Astra.Collections.Recyclable;

public interface IObjectPool<T> where T : IRecyclable
{
    public void Return(T subject);
    public T Retrieve();
}