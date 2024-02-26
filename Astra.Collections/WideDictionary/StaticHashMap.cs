using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Astra.Collections.WideDictionary;

[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Length = {LongLength}, Capacity = {Capacity}, HashPower = {HashPower}")]
public class StaticHashMap<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private class Element
    {
        public ulong Hash { get; set; }
        public Element? Next { get; set; }
        public KeyValuePair<TKey, TValue> Pair { get; set; }
        public TKey Key => Pair.Key;
        public TValue Value => Pair.Value;
    }

    private readonly struct IteratorLock : IDisposable
    {
        private readonly StaticHashMap<TKey, TValue>? _host;

        public IteratorLock(int _)
        {
            _host = null;
        }
        
        public IteratorLock(StaticHashMap<TKey, TValue> host)
        {
            host.CheckIterating();
            _host = host;
            _host._iterating = true;
        }
        public void Dispose()
        {
            if (_host != null)
                _host._iterating = false;
        }
    }
    
    public struct Iterator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private CopyIterator _copyIterator;

        public Iterator(int _)
        {
            _copyIterator = new(0);
        }
        
        public Iterator(StaticHashMap<TKey, TValue> host)
        {
            _copyIterator = new(host);
        }

        public void Dispose()
        {
            _copyIterator.Dispose();
        }

        public bool MoveNext() => _copyIterator.MoveNext();

        public void Reset() => _copyIterator.Reset();

        public KeyValuePair<TKey, TValue> Current => _copyIterator.Current.Pair;

        object IEnumerator.Current => Current;
    }
    
    private struct CopyIterator : IEnumerator<Element>
    {
        private readonly StaticHashMap<TKey, TValue> _host;
        private readonly IteratorLock _lock;
        private readonly long _size;
        private readonly long _capacity;
        private long _iterated;
        private long _index = -1;
        private Element? _element;

        public CopyIterator(int _)
        {
            _host = null!;
            _lock = new(0);
            _size = 0;
            _capacity = 0;
            _iterated = 0;
            _element = null;
        }
        
        public CopyIterator(StaticHashMap<TKey, TValue> host)
        {
            _lock = new(host);
            _host = host;
            _size = _host._size;
            _capacity = _host.Capacity;
            _element = null;
        }
        
        public void Dispose()
        {
            _lock.Dispose();
        }

        public bool MoveNext()
        {
            if (_iterated >= _size) return false;
            if (_element == null)
            {
                while (_element == null)
                {
                    _element = _host._elements[++_index];
                    if (_index >= _capacity) return false;
                }

                _iterated++;
                return true;
            }

            _element = _element.Next;
            if (_element != null)
            {
                _iterated++;
                return true;
            }
            while (_element == null)
            {
                _element = _host._elements[++_index];
                if (_index >= _capacity) return false;
            }
            _iterated++;
            return true;
        }

        public void Reset()
        {
            _host._iterating = true;
            _element = null;
            _iterated = 0;
            _index = -1;
        }

        public Element Current => _element!;

        object IEnumerator.Current => Current;
    }

    private readonly Element[] _elements;
    private readonly IHasher<TKey> _hasher;
    private readonly int _hashPower;
    private long _size;
    private bool _iterating;

    public int HashPower => _hashPower;
    public long Capacity => _elements.LongLength;
    private ulong InternalCapacity => unchecked((ulong)_elements.LongLength);
    public long LongLength => _size;

    public StaticHashMap(int hashPower, IHasher<TKey> hasher)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hashPower, 62);
        _hashPower = hashPower;
        _elements = new Element[1L << hashPower];
        _hasher = hasher;
    }

    public StaticHashMap(int hashPower) : this(hashPower, WideHasher<TKey>.Default)
    {
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIterating()
    {
        if (_iterating) throw new InvalidOperationException("This HashMap is being iterated");
    }
    
    private Element? GetElement(TKey key)
    {
        var hash = _hasher.Hash(key);
        var index = hash & (InternalCapacity - 1);
        var e = _elements[index];
        while (e != null)
        {
            if (e.Hash == hash && _hasher.Equals(key, e.Key))
                return e;
            e = e.Next;
        }

        return null;
    }

    private void CreateElement(KeyValuePair<TKey, TValue> pair)
    {
        using var @lock = new IteratorLock(this);
        var hash = _hasher.Hash(pair.Key);
        var index = hash & (InternalCapacity - 1);
        var e = new Element
        {
            Hash = hash,
            Pair = pair,
            Next = _elements[index]
        };
        _elements[index] = e;
        _size++;
    }

    public bool ContainsKey(TKey key) => GetElement(key) != null;


    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var e = GetElement(key);
        if (e == null)
        {
            value = default;
            return false;
        }

        value = e.Value;
        return true;
    }

    public TValue this[TKey key]
    {
        get
        {
            var e = GetElement(key);
            if (e == null) throw new KeyNotFoundException();
            return e.Value;
        }
        set => Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public ICollection<TKey> Keys => this.Select(o => o.Key).ToArray();
    public ICollection<TValue> Values => this.Select(o => o.Value).ToArray();

    public void Add(TKey key, TValue value)
    {
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool Remove(TKey key)
    {
        using var @lock = new IteratorLock(this);
        var hash = _hasher.Hash(key);
        var index = hash & (InternalCapacity - 1);
        var e = _elements[index];
        Element? p = null;
        while (e != null)
        {
            if (e.Hash == hash && _hasher.Equals(e.Key, key))
            {
                if (p != null)
                {
                    p.Next = e.Next;
                }
                else
                {
                    _elements[index] = e.Next!;
                }

                _size--;
                return true;
            }

            p = e;
            e = e.Next;
        }

        return false;
    }

    public void CopyFrom(StaticHashMap<TKey, TValue> other)
    {
        using var @lock = new IteratorLock(this);
        var iterator = other.GetCopyEnumerator();
        try
        {
            while (iterator.MoveNext())
            {
                Add(iterator.Current);
            }
        }
        finally
        {
            iterator.Dispose();
        }
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Iterator GetEnumerator()
    {
        return new(this);
    }

    private CopyIterator GetCopyEnumerator()
    {
        return new(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        var e = GetElement(item.Key);
        if (e == null)
        {
            CreateElement(item);
            return;
        }

        e.Pair = item;
    }

    private void Add(Element from)
    {
        var hash = from.Hash;
        var index = hash & (InternalCapacity - 1);
        var e = new Element
        {
            Hash = hash,
            Pair = from.Pair,
            Next = _elements[index]
        };
        _elements[index] = e;
        _size++;
    }

    public void Clear()
    {
        using var @lock = new IteratorLock(this);
        Array.Clear(_elements);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return ContainsKey(item.Key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var pair in this)
        {
            if (arrayIndex >= array.Length) return;
            array[arrayIndex++] = pair;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public int Count => (int)LongLength;
    public bool IsReadOnly => false;
}