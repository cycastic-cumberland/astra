namespace Astra.Engine;

public interface IEnumerable<out T, out TEnumerator> : IEnumerable<T> where TEnumerator : IEnumerator<T>
{
    public new TEnumerator GetEnumerator();
}