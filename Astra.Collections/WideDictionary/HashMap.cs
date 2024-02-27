using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections.WideDictionary;

[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Length = {LongLength}, Capacity = {Capacity}, HashPower = {HashPower}")]
public class HashMap<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private const int DefaultHashPower = 4;
    private StaticHashMap<TKey, TValue>? _map;
    private readonly IHasher<TKey> _hasher;
    private readonly double _growthFactor;
    private long _nextMigration;

    public int HashPower => _map?.HashPower ?? 0;
    public long Capacity => _map?.Capacity ?? 0;
    public long LongLength => _map?.LongLength ?? 0;
    public double GrowthFactor => _growthFactor;

    public HashMap(double growthFactor, IHasher<TKey> hasher)
    {
        _growthFactor = growthFactor;
        _hasher = hasher;
    }
    
    public HashMap(double growthFactor)
        : this(growthFactor, WideHasher<TKey>.Default)
    {
        
    }

    public HashMap()
        : this(2.0)
    {
        
    }

    private StaticHashMap<TKey, TValue> CreateNewMap(int power)
    {
        return new(power, _hasher);
    }

    private StaticHashMap<TKey, TValue> Migrate(StaticHashMap<TKey, TValue> oldMap)
    {
        var newMap = CreateNewMap(oldMap.HashPower + 1);
        _nextMigration = (long)(newMap.Capacity * _growthFactor);
        newMap.CopyFrom(oldMap);
        return newMap;
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public StaticHashMap<TKey, TValue>.Iterator GetEnumerator()
    {
        if (_map == null) return new(0);
        return _map.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        if (_map == null)
        {
            _map = CreateNewMap(DefaultHashPower);
            _nextMigration = (long)(_map.Capacity * _growthFactor); 
            _map.Add(item);
            return;
        }
        
        _map.Add(item);
        if (_map.LongLength > _nextMigration)
            _map = Migrate(_map);
    }

    public void Clear()
    {
        _map = null;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _map?.ContainsKey(item.Key) ?? false;
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        _map?.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return _map?.Remove(item.Key) ?? false;
    }

    public int Count => (int)LongLength;
    public bool IsReadOnly => false;
    
    public void Add(TKey key, TValue value)
    {
        Add(new(key, value));
    }

    public bool ContainsKey(TKey key)
    {
        return _map?.ContainsKey(key) ?? false;
    }

    public bool Remove(TKey key)
    {
        return _map?.Remove(key) ?? false;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_map == null)
        {
            value = default;
            return false;
        }

        return _map.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (_map == null) throw new KeyNotFoundException();
            return _map[key];
        }
        set => Add(new(key, value));
    }

    public ICollection<TKey> Keys => _map?.Keys ?? ArraySegment<TKey>.Empty;
    public ICollection<TValue> Values => _map?.Values ?? ArraySegment<TValue>.Empty;
}