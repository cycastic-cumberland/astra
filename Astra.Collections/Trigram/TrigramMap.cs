using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections.Trigram;

public class TrigramMap<TUnit, TKey, TValue> : ITrigramMap<TKey, TValue>
    where TUnit : IEquatable<TUnit>
    where TKey : IReadOnlyList<TUnit>
{
    private readonly Dictionary<Trigram<TUnit>, Dictionary<TKey, TValue>> _fuzzyDictionary = new();
    private readonly Dictionary<TKey, TValue> _masterDictionary = new();
    private readonly TUnit _defaultUnit;

    public TrigramMap(TUnit defaultUnit)
    {
        _defaultUnit = defaultUnit;
    }
    
    public TrigramMap() : this(default!) {}
    
    public void Add(TKey key, TValue value)
    {
        if (!_masterDictionary.TryAdd(key, value)) return;
        foreach (var trigram in key.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var dict))
            {
                dict = new();
                _fuzzyDictionary[trigram] = dict;
            }
            
            dict[key] = value;
        }
    }

    public bool ContainsKey(TKey key)
    {
        return _masterDictionary.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return TryRemove(key, out _);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _masterDictionary.TryGetValue(key, out value);
    }

    public TValue this[TKey key]
    {
        get => _masterDictionary[key];
        set => Add(key, value);
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys => _masterDictionary.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => _masterDictionary.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;
    ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!_masterDictionary.Remove(key, out value)) return false;
        
        foreach (var trigram in key.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var dict))
            {
                continue;
            }
            
            dict.Remove(key);
        }
        
        return true;
    }

    public Dictionary<TKey, (TValue value, int matchedLength)> FuzzySearch(TKey key, int lengthFilter = -1)
    {
        var allMatches = new Dictionary<TKey, (TValue value, int matchedLength)>();
        var sufficientMatches = new Dictionary<TKey, (TValue value, int matchedLength)>();
        foreach (var trigram in key.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var set)) continue;
            foreach (var pair in set)
            {
                var length = !allMatches.TryGetValue(pair.Key, out var existingRecord) 
                    ? 0 : existingRecord.matchedLength;

                existingRecord = (pair.Value, length + trigram.Length);
                allMatches[pair.Key] = existingRecord;
                if (length >= lengthFilter)
                {
                    sufficientMatches[pair.Key] = existingRecord;
                }
            }
        }

        return sufficientMatches;
    }
    
    IEnumerable<KeyValuePair<TKey, (TValue value, int matchedLength)>> ITrigramMap<TKey, TValue>.FuzzySearch(TKey key, int lengthFilter)
    {
        return FuzzySearch(key);
    }

    public Dictionary<TKey,TValue>.Enumerator GetEnumerator() => _masterDictionary.GetEnumerator();
    
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _masterDictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _masterDictionary.Clear();
        _fuzzyDictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return _masterDictionary.ContainsKey(item.Key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return TryRemove(item.Key, out _);
    }

    public int Count => _masterDictionary.Count;
    public bool IsReadOnly => false;
}

