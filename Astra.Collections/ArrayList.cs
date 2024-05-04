using System.Buffers;

namespace Astra.Collections;

public struct ArrayList<T> : IDisposable
{
    private const int DefaultCapacity = 8;
    private T[] _array;
    private int _index;

    public int Capacity => _array.Length;
    public int Count => _index;

    public ArrayList()
    {
        _array = Array.Empty<T>();
    }

    public ArrayList(T[] array, int length)
    {
        _array = array;
        _index = length;
    }

    public ArrayList(int minimumCapacity)
    {
        _array = ArrayPool<T>.Shared.Rent(minimumCapacity);
    }
    
    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array);
    }
    
    public void Consume(out T[] array, out int length)
    {
        array = _array;
        length = _index;
        _array = Array.Empty<T>();
        _index = 0;
    }

    private void Grow()
    {
        if (_array.Length == 0)
        {
            _array = ArrayPool<T>.Shared.Rent(DefaultCapacity);
            return;
        }

        var newCapacity = _array.Length * 2;
        var newArray = ArrayPool<T>.Shared.Rent(newCapacity);
        if (_array.Length < 128)
        {
            for (var i = 0; i < _array.Length; i++)
            {
                newArray[i] = _array[i];
            }
        }
        else
        {
            Array.Copy(_array, 0, newArray, 0, _array.Length);
        }
        Dispose();
        _array = newArray;
    }

    public void Add(T item)
    {
        if (_index >= _array.Length) Grow();
        _array[_index++] = item;
    }
}