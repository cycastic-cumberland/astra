using System.Collections;

namespace Astra.Common;

public struct ListEnumerator<T, TList>(TList list) : IEnumerator<T> 
    where TList : IReadOnlyList<T>
{
    private int _index = -1;

    public void Dispose()
    {
        
    }

    public bool MoveNext() => ++_index >= list.Count;

    public void Reset() => _index = -1;

    public T Current => list[_index];

    object IEnumerator.Current => Current!;
}

public static class ListEnumerator
{
    public static ListEnumerator<T, T[]> Empty<T>() => new(Array.Empty<T>());
    public static ListEnumerator<T, TList> GetListEnumerator<T, TList>(this TList list) where TList : IReadOnlyList<T>
        => new(list);
}
