using System.Collections;

namespace Astra.Collections;

file class PairsIterator<TKey, TValue>(ReadOnlyPairs<TKey, TValue> host)
    : IEnumerator<KeyValuePair<TKey, TValue>>
{
    private int _curr = - 1;

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        _curr++;
        return _curr < host.Count;
    }

    public void Reset()
    {
        _curr = -1;
    }

    public KeyValuePair<TKey, TValue> Current => host[_curr];

    object IEnumerator.Current => Current;
}

file class KeysIterator<TKey, TValue>(ReadOnlyPairs<TKey, TValue> host)
    : IEnumerator<TKey>
{
    private int _curr = - 1;

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        _curr++;
        return _curr < host.Count;
    }

    public void Reset()
    {
        _curr = -1;
    }

    public TKey Current => host[_curr].Key;

    object IEnumerator.Current => Current!;
}

file class ValuesIterator<TKey, TValue>(ReadOnlyPairs<TKey, TValue> host)
    : IEnumerator<TValue>
{
    private int _curr = - 1;

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        _curr++;
        return _curr < host.Count;
    }

    public void Reset()
    {
        _curr = -1;
    }

    public TValue Current => host[_curr].Value;

    object IEnumerator.Current => Current!;
}

file static class Helper
{
    internal static PairsIterator<TKey, TValue> GetPairsIterator<TKey, TValue>(this ReadOnlyPairs<TKey, TValue> pairs)
    {
        return new PairsIterator<TKey, TValue>(pairs);
    }
    
    internal static KeysIterator<TKey, TValue> GetKeysIterator<TKey, TValue>(this ReadOnlyPairs<TKey, TValue> pairs)
    {
        return new KeysIterator<TKey, TValue>(pairs);
    }
    
    internal static ValuesIterator<TKey, TValue> GetValuesIterator<TKey, TValue>(this ReadOnlyPairs<TKey, TValue> pairs)
    {
        return new ValuesIterator<TKey, TValue>(pairs);
    }
}

public readonly struct ReadOnlyKeys<TKey, TValue>(ReadOnlyPairs<TKey, TValue> host) : ICollection<TKey>
{
    public IEnumerator<TKey> GetEnumerator()
    {
        return host.GetKeysIterator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(TKey item)
    {
        throw new NotSupportedException();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(TKey item)
    {
        var comparator = EqualityComparer<TKey>.Default;
        var enumerator = host.GetKeysIterator();
        try
        {
            while (enumerator.MoveNext())
                if (comparator.Equals(enumerator.Current, item))
                    return true;
        }
        finally
        {
            enumerator.Dispose();
        }

        return false;
    }

    public void CopyTo(TKey[] array, int arrayIndex)
    {
        var enumerator = host.GetKeysIterator();
        try
        {
            var index = arrayIndex;
            while (enumerator.MoveNext())
                array[index++] = enumerator.Current;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    public bool Remove(TKey item)
    {
        throw new NotSupportedException();
    }

    public int Count => host.Count;
    public bool IsReadOnly => true;
}

public readonly struct ReadOnlyValues<TKey, TValue>(ReadOnlyPairs<TKey, TValue> host) : ICollection<TValue>
{
    public IEnumerator<TValue> GetEnumerator()
    {
        return host.GetValuesIterator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(TValue item)
    {
        throw new NotSupportedException();
    }

    public void Clear()
    {
        throw new NotSupportedException();
    }

    public bool Contains(TValue item)
    {
        var comparator = EqualityComparer<TValue>.Default;
        var enumerator = host.GetValuesIterator();
        try
        {
            while (enumerator.MoveNext())
                if (comparator.Equals(enumerator.Current, item))
                    return true;
        }
        finally
        {
            enumerator.Dispose();
        }

        return false;
    }

    public void CopyTo(TValue[] array, int arrayIndex)
    {
        var enumerator = host.GetValuesIterator();
        try
        {
            var index = arrayIndex;
            while (enumerator.MoveNext())
                array[index++] = enumerator.Current;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    public bool Remove(TValue item)
    {
        throw new NotSupportedException();
    }

    public int Count => host.Count;
    public bool IsReadOnly => true;
}

public readonly struct ReadOnlyPairs<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly KeyValuePair<TKey, TValue>[] _pairs;
    private readonly int _start;
    private readonly int _length;

    public int Count => _length;

    public KeyValuePair<TKey, TValue> this[int index] => _pairs[_start + index];

    public ReadOnlyKeys<TKey, TValue> Keys => new(this);
    public ReadOnlyValues<TKey, TValue> Values => new(this);

    public ReadOnlyPairs(KeyValuePair<TKey, TValue>[]? pairs, int start, int length)
    {
        if (pairs == null)
        {
            if (start != 0 || length != 0) throw new NullReferenceException(nameof(pairs));
            pairs = Array.Empty<KeyValuePair<TKey, TValue>>();
        }
        
        if (start + length > (uint)pairs.Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        _pairs = pairs;
        _start = start;
        _length = length;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return this.GetPairsIterator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}