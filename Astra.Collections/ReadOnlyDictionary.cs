using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections;

public readonly struct ReadOnlyDictionary<TDict, TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TDict : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly TDict _dict;

    public ReadOnlyDictionary(TDict dict)
    {
        _dict = dict;
    }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dict).GetEnumerator();
    }

    public int Count => _dict.Count;

    public bool ContainsKey(TKey key)
    {
        return _dict.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _dict.TryGetValue(key, out value);
    }

    public TValue this[TKey key] => _dict[key];

    public IEnumerable<TKey> Keys => _dict.Keys;

    public IEnumerable<TValue> Values => _dict.Values;
}