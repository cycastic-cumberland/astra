using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Engine;

public interface IAutoSerial<TKey, TValue> : IReadOnlyDictionary<ulong, TValue> where TKey: struct, IEquatable<TKey> 
{
    public TKey CurrentId { get; }

    public TKey Save(TValue value);
    public bool Remove(TKey id, [MaybeNullWhen(false)] out TValue ret);
    public TKey Exchange(TKey oldItermId, TValue newItem, out TValue? oldItem);
    public void Clear();
}

public class AutoSerial<T> : IAutoSerial<ulong, T>
{
    private readonly Dictionary<ulong, T> _dict = new();
    private ulong _currentId;

    public ulong CurrentId => _currentId;
    public int Count => _dict.Count;

    public ulong Save(T value)
    {
        _dict[++_currentId] = value;
        return _currentId;
    }

    public bool Remove(ulong id, [MaybeNullWhen(false)] out T ret)
    {
        _dict.TryGetValue(id, out ret);
        return _dict.Remove(id);
    }

    public ulong Exchange(ulong oldItermId, T newItem, out T? oldItem)
    {
        Remove(oldItermId, out oldItem);
        return Save(newItem);
    }

    public void Clear()
    {
        _dict.Clear();
    }

    public bool ContainsKey(ulong key)
    {
        return _dict.ContainsKey(key);
    }

    public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out T value)
    {
        return _dict.TryGetValue(key, out value);
    }

    public T this[ulong id] => _dict[id];
    public IEnumerable<ulong> Keys => _dict.Keys;

    public IEnumerable<T> Values => _dict.Values;

    public IEnumerator<KeyValuePair<ulong, T>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dict).GetEnumerator();
    }
}

public class ConcurrentAutoSerial<T> : IAutoSerial<ulong, T>
{
    private readonly ConcurrentDictionary<ulong, T> _dict = new();
    private LongSequence _longSequence;

    public ulong CurrentId => _longSequence.Current;
    public int Count => _dict.Count;

    public ulong Save(T value)
    {
        _dict[_longSequence.Next] = value;
        return CurrentId;
    }

    public bool Remove(ulong id, [MaybeNullWhen(false)] out T ret)
    {
        return _dict.TryRemove(id, out ret);
    }

    public ulong Exchange(ulong oldItermId, T newItem, out T? oldItem)
    {
        Remove(oldItermId, out oldItem);
        return Save(newItem);
    }

    public void Clear()
    {
        _dict.Clear();
    }

    public bool ContainsKey(ulong key)
    {
        return _dict.ContainsKey(key);
    }

    public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out T value)
    {
        return _dict.TryGetValue(key, out value);
    }

    public T this[ulong id] => _dict[id];
    public IEnumerable<ulong> Keys => _dict.Keys;

    public IEnumerable<T> Values => _dict.Values;

    public IEnumerator<KeyValuePair<ulong, T>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dict).GetEnumerator();
    }
}
