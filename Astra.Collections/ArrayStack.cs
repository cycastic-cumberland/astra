using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections;

public struct ArrayStack<T> : IDisposable, ICollection<T>, IReadOnlyCollection<T>
{
    private readonly T[] _realArray;
    private readonly Memory<T> _memory;
    private Span<T> Span => _memory.Span;
    private int _cursor;

    public ArrayStack(int capacity)
    {
        _realArray = capacity <= 0 ? Array.Empty<T>() : ArrayPool<T>.Shared.Rent(capacity);
        _memory = new(_realArray, 0, capacity);
    }
    
    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_realArray);
    }

    public bool IsEmpty => _cursor <= 0;

    public void Push(T item)
    {
        Span[_cursor++] = item;
    }

    public T Pop()
    {
        return Span[--_cursor];
    }

    public bool TryPop([MaybeNullWhen(false)] out T value)
    {
        if (_cursor == 0)
        {
            value = default;
            return false;
        }

        value = Span[--_cursor];
        return true;
    }
    
    public T Peek()
    {
        return Span[_cursor - 1];
    }

    public bool TryPeek([MaybeNullWhen(false)] out T value)
    {
        if (_cursor == 0)
        {
            value = default;
            return false;
        }

        value = Span[_cursor - 1];
        return true;
    }

    public void Add(T item)
    {
        Push(item);
    }

    public void Clear()
    {
        _cursor = 0;
    }

    public bool Contains(T item)
    {
        var comparator = EqualityComparer<T>.Default;
        for (var i = 0; i < _cursor; i++)
        {
            if (!comparator.Equals(Span[i], item)) continue;
            return true;
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        for (var i = 0; i < _cursor; i++)
            array[arrayIndex + i] = Span[i];
    }

    public bool Remove(T item)
    {
        var comparator = EqualityComparer<T>.Default;
        for (var i = 0; i < _cursor; i++)
        {
            if (!comparator.Equals(Span[i], item)) continue;
            for (var j = i; j < --_cursor; j++)
                Span[j] = Span[j + 1];
            return true;
        }

        return false;
    }

    int ICollection<T>.Count => _cursor;

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _cursor; i++)
            yield return Span[i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    int IReadOnlyCollection<T>.Count => _cursor;
}