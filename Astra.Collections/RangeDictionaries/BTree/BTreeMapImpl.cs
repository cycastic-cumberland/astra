using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections.RangeDictionaries.BTree;

[DebuggerTypeProxy(typeof(BTreeDebugView<,>))]
[DebuggerDisplay("Count = {Count}, Degree = {Degree}")]
public sealed partial class BTreeMap<TKey, TValue>
{
    private const int MinDegree = BTreeMap.MinDegree;
    // private ulong _structureVersion;
    private readonly int _degree;
    private INode? _root;
    private long _elementCount;
    private int _depth;

    public int Count => (int)_elementCount;
    public long LongCount => _elementCount;
    public int Degree => _degree;
    public int Depth => _depth;

    internal INode? Root => _root;

    public BTreeMap(int degree)
    {
        if (degree < MinDegree) throw new NotSupportedException($"{nameof(degree)} must be {MinDegree} or greater");
        _degree = degree;
    }

    private void Insert(TKey key, TValue value)
    {
        if (_root == null)
        {
            var leaf = new LeafNode(_degree, new(key, value));
            _root = leaf;
            _elementCount++;
            _depth = 1;
            // _structureVersion++;
            return;
        }

        var ret = _root.Insert(key, value);
        switch (ret.Result)
        {
            case InsertionResult.NoSizeChange:
                break;
            case InsertionResult.SizeChanged:
            {
                _elementCount++;
                // _structureVersion++;
                break;
            }
            case InsertionResult.NodeSplit:
            {
                _elementCount++;
                // _structureVersion++;
                var newNode = ret.Node;
                var newRoot = new InternalNode(_degree, _root, newNode);
                _root = newRoot;
                _depth++;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_root == null)
        {
            value = default;
            return false;
        }

        var ret = _root.Remove(key);
        switch (ret.Result)
        {
            case RemovalResult.Empty:
            {
                _root = null;
                _depth = 0;
                goto case RemovalResult.ReturningInternal;
            }
            case RemovalResult.NoSizeChange:
            {
                value = default;
                return false;
            }
            case RemovalResult.SizeChanged:
            {
                if (_root.KeyCount == 1 && _root.IsInternal)
                {
                    _root = ((InternalNode)_root).Children[0];
                }
                goto case RemovalResult.ReturningInternal;
            }
            case RemovalResult.NodesMerged:
            {
                var root = (InternalNode)_root;
                if (root.ChildCount == 1)
                {
                    _root = root.Children[0];
                    _depth--;
                }

                goto case RemovalResult.ReturningInternal;
            }
            case RemovalResult.ReturningInternal:
            {
                _elementCount--;
                value = ret.Value;
                return true;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool Remove(TKey key)
    {
        return TryRemove(key, out _);
    }

    public bool Contains(TKey key)
    {
        return _root?.Contains(key) ?? false;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = default;
        return _root?.TryGetValue(key, out value) ?? false;
    }

    public void Clear()
    {
        _elementCount = 0;
        _depth = 0;
        _root = null;
    }
    
    public NodeEnumerator GetEnumerator()
    {
        return _root == null ? NodeEnumerator.Empty : new NodeEnumerator(_root, _depth);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public IEnumerable<KeyValuePair<TKey, TValue>> Collect(TKey fromBound, TKey toBound, CollectionMode mode)
    {
        if (toBound < fromBound)
            (toBound, fromBound) = (fromBound, toBound);
        return _root == null 
            ? ArraySegment<KeyValuePair<TKey, TValue>>.Empty 
            : _root.Collect(_depth, fromBound, toBound, mode);
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> CollectFrom(TKey fromBound, bool inclusive = true)
    {
        return inclusive
            ? Collect(fromBound, NumericHelper.GetMax<TKey>(), CollectionMode.ClosedInterval)
            : Collect(fromBound == NumericHelper.GetMax<TKey>() 
                ? fromBound 
                : fromBound + NumericHelper.GetEpsilon<TKey>(), NumericHelper.GetMax<TKey>(), CollectionMode.ClosedInterval);
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> CollectTo(TKey toBound, bool inclusive = true)
    {
        return inclusive
            ? Collect(NumericHelper.GetMin<TKey>(), toBound, CollectionMode.ClosedInterval)
            : Collect(NumericHelper.GetMin<TKey>(), toBound == NumericHelper.GetMin<TKey>()
                ? toBound
                : toBound - NumericHelper.GetEpsilon<TKey>(), CollectionMode.ClosedInterval);
    }
    
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Insert(item.Key, item.Value);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return Contains(item.Key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        int i = arrayIndex;
        foreach (var kp in this)
        {
            array[i++] = kp;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return TryRemove(item.Key, out _);
    }

    public bool IsReadOnly => false;
    public void Add(TKey key, TValue value)
    {
        Insert(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return Contains(key);
    }

    public TValue this[TKey key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
                throw new KeyNotFoundException();
            return value;
        }
        set => Insert(key, value);
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _root?.Keys ?? ArraySegment<TKey>.Empty;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _root?.Values ?? ArraySegment<TValue>.Empty;

    public ICollection<TKey> Keys => (_root?.Keys ?? ArraySegment<TKey>.Empty).ToArray();
    public ICollection<TValue> Values => (_root?.Values ?? ArraySegment<TValue>.Empty).ToArray();
    public IEnumerable<KeyValuePair<TKey, TValue>> CollectBetween(TKey fromValue, TKey toValue, bool includeFrom = true, bool includeTo = true)
    {
        var mode = includeFrom && includeTo
            ? CollectionMode.ClosedInterval
            : includeFrom && !includeTo
                ? CollectionMode.HalfClosedLeftInterval
                : !includeFrom && includeTo
                    ? CollectionMode.HalfClosedRightInterval
                    : CollectionMode.OpenInterval;
        return Collect(fromValue, toValue, mode);
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> CollectExclude(TKey fromValue, TKey toValue, bool includeFrom = true, bool includeTo = true)
    {
        throw new NotSupportedException();
        // var mode = includeFrom && includeTo
        //     ? CollectionMode.UnboundedClosedInterval
        //     : includeFrom && !includeTo
        //         ? CollectionMode.UnboundedHalfClosedLeftInterval
        //         : !includeFrom && includeTo
        //             ? CollectionMode.UnboundedHalfClosedRightInterval
        //             : CollectionMode.UnboundedOpenInterval;
        // return Collect(fromValue, toValue, mode);
    }

    public void Traverse<T>(T traversable) where T : IBTreeTraversable<TKey, TValue>
    {
        traversable.Start(_elementCount, _degree);
        _root?.Traverse(traversable);
        traversable.Finish();
    }

    public int EnclosedCollect(TKey fromBound, TKey toBound, CollectionMode mode, Func<KeyValuePair<TKey, TValue>, bool> collector)
    {
        var collected = 0;
        foreach (var kp in Collect(fromBound, toBound, mode))
        {
            collected++;
            if (!collector(kp)) break;
        }

        return collected;
    }
}