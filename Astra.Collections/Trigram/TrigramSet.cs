using System.Collections;

namespace Astra.Collections.Trigram;

public class TrigramSet<TUnit, TSequence> : ISet<TSequence>
    where TUnit : IEquatable<TUnit>
    where TSequence : IReadOnlyList<TUnit>
{
    private readonly Dictionary<Trigram<TUnit>, HashSet<TSequence>> _fuzzyDictionary = new();
    private readonly HashSet<TSequence> _masterSet = new();
    private readonly TUnit _defaultUnit;

    public TrigramSet(TUnit defaultUnit)
    {
        _defaultUnit = defaultUnit;
    }
    
    public TrigramSet() : this(default!) {}
    public bool Add(TSequence sequence)
    {
        if (!_masterSet.Add(sequence)) return false;
        foreach (var trigram in sequence.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var set))
            {
                set = new();
                _fuzzyDictionary[trigram] = set;
            }

            set.Add(sequence);
        }
        

        return true;
    }

    public void ExceptWith(IEnumerable<TSequence> other)
    {
        _masterSet.ExceptWith(other);
    }

    public void IntersectWith(IEnumerable<TSequence> other)
    {
        _masterSet.IntersectWith(other);
    }

    public bool IsProperSubsetOf(IEnumerable<TSequence> other)
    {
        return _masterSet.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<TSequence> other)
    {
        return _masterSet.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<TSequence> other)
    {
        return _masterSet.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<TSequence> other)
    {
        return _masterSet.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<TSequence> other)
    {
        return _masterSet.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<TSequence> other)
    {
        return _masterSet.SetEquals(other);
    }

    public void SymmetricExceptWith(IEnumerable<TSequence> other)
    {
        _masterSet.SymmetricExceptWith(other);
    }

    public void UnionWith(IEnumerable<TSequence> other)
    {
        _masterSet.UnionWith(other);
    }

    public bool Remove(TSequence sequence)
    {
        if (!_masterSet.Remove(sequence)) return false;
        
        foreach (var trigram in sequence.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var set))
            {
                continue;
            }
            
            set.Remove(sequence);
        }
        
        return true;
    }

    public int Count => _masterSet.Count;

    public bool IsReadOnly => false;

    public IEnumerable<TSequence> AsynchronousFuzzySearch(TSequence inputSequence)
    {
        foreach (var trigram in inputSequence.ToTrigrams(_defaultUnit))
        {
            if (!_fuzzyDictionary.TryGetValue(trigram, out var set)) continue;
            foreach (var pair in set)
            {
                yield return pair;
            }
        }
    }

    public HashSet<TSequence>.Enumerator GetEnumerator() => _masterSet.GetEnumerator();

    IEnumerator<TSequence> IEnumerable<TSequence>.GetEnumerator() => _masterSet.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection<TSequence>.Add(TSequence item)
    {
        _ = Add(item);
    }

    public void Clear()
    {
        _masterSet.Clear();
        _fuzzyDictionary.Clear();
    }

    public bool Contains(TSequence sequence)
    {
        return _masterSet.Contains(sequence);
    }
    
    public void CopyTo(TSequence[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }
}
