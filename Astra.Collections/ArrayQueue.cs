using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections;

public struct ArrayQueue<T> : IDisposable
{
    private readonly T[] _array;
    private int _head;
    private int _tail;

    public int Count => _tail - _head;
    public int Capacity => _array.Length;
    public bool IsFull => Count == Capacity;
    public bool IsEmpty => Count == 0;
    public ReadOnlySpan<T> Span => new(_array, _head, _tail);

    public ArrayQueue(int minimumCapacity)
    {
        _array = ArrayPool<T>.Shared.Rent(minimumCapacity);
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array);
    }

    public void Clear()
    {
        _head = _tail = 0;
    }

    private void Relocate()
    {
        var size = Count;
        for (var i = 0; i < size; i++)
        {
            _array[i] = _array[i + _head];
        }

        _tail -= _head;
        _head = 0;
    }

    public bool TryEnqueue(T item)
    {
        if (_tail >= _array.Length)
        {
            if (_head > 0) Relocate();
            else return false;
        }
        _array[_tail++] = item;
        return true;
    }

    public void Enqueue(T item)
    {
        if (!TryEnqueue(item))
            throw new InternalBufferOverflowException();
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (_tail == _head)
        {
            if (_head > 0) Relocate();
            else
            {
                item = default;
                return false;
            }
        }

        item = _array[--_tail];
        return true;
    }

    public T Dequeue()
    {
        if (TryDequeue(out var item)) return item;
        throw new InvalidOperationException();
    }
}