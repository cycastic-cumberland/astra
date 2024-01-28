using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Astra.Collections.RangeDictionaries.BTree;

public sealed partial class BTreeMap<TKey, TValue>
{
    public const int MinDegree = 2;
    // private ulong _structureVersion;
    private readonly int _degree;
    private INode? _root;
    private int _elementCount;

    public int Count => _elementCount;

    public BTreeMap(int degree)
    {
        if (degree < MinDegree) throw new NotSupportedException($"degree must be {MinDegree} or greater");
        _degree = degree;
    }

    public void Insert(TKey key, TValue value)
    {
        if (_root == null)
        {
            var leaf = new LeafNode(_degree, new(key, value));
            _root = leaf;
            _elementCount++;
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
        _root = null;
    }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        if (_root == null)
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)ArraySegment<KeyValuePair<TKey, TValue>>.Empty)
                .GetEnumerator();
        return _root.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<KeyValuePair<TKey, TValue>> Collect(TKey fromBound, TKey toBound, CollectionMode mode)
    {
        if (toBound < fromBound)
            throw new ArgumentException($"{nameof(fromBound)} must be lower than {nameof(toBound)}");
        return _root == null 
            ? ArraySegment<KeyValuePair<TKey, TValue>>.Empty 
            : _root.Collect(fromBound, toBound, mode);
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
}