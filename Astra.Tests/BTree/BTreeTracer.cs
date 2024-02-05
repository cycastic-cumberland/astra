using System.Collections;
using System.Diagnostics;
using System.Numerics;
using Astra.Collections.RangeDictionaries.BTree;

namespace Astra.Tests.BTree;

[DebuggerTypeProxy(typeof(BTreeTracerDebugView<,>))]
public class BTreeTracer<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : INumber<TKey>
{
    internal interface ITracer
    {
        public void Reconstruct(BTreeMap<TKey, TValue> tree);
    }
    
    [DebuggerTypeProxy(typeof(InsertTracerProxy<,>))]
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    internal class InsertTracer(TKey key, TValue value) : ITracer
    {
        internal TKey Key => key;
        internal TValue Value => value;
        public void Reconstruct(BTreeMap<TKey, TValue> tree)
        {
            tree[key] = value;
        }
    }

    [DebuggerTypeProxy(typeof(DeleteTracerProxy<,>))]
    [DebuggerDisplay("Key = {Key}")]
    internal class DeleteTracer(TKey key) : ITracer
    {
        internal TKey Key => key;
        public void Reconstruct(BTreeMap<TKey, TValue> tree)
        {
            tree.Remove(key);
        }
    }
    

    internal class ClearTracer : ITracer
    {
        public static readonly ClearTracer Default = new();
        public void Reconstruct(BTreeMap<TKey, TValue> tree)
        {
            tree.Clear();
        }
    }

    internal readonly List<ITracer> _tracers  = new();
    private readonly int _degree;

    public BTreeTracer(int degree) => _degree = degree;
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        this[item.Key] = item.Value;
    }

    public void Clear()
    {
        _tracers.Add(ClearTracer.Default);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        throw new NotSupportedException();
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Remove(item.Key);
    }

    public int Count => throw new NotSupportedException();
    public bool IsReadOnly { get; } = false;
    public void Add(TKey key, TValue value)
    {
        _tracers.Add(new InsertTracer(key, value));
    }

    public bool ContainsKey(TKey key)
    {
        throw new NotSupportedException();
    }

    public bool Remove(TKey key)
    {
        _tracers.Add(new DeleteTracer(key));
        return true;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        throw new NotSupportedException();
    }

    public TValue this[TKey key]
    {
        get => throw new NotSupportedException();
        set => Add(key, value);
    }

    public ICollection<TKey> Keys => throw new NotSupportedException();
    public ICollection<TValue> Values => throw new NotSupportedException();

    public BTreeMap<TKey, TValue> Reconstruct()
    {
        var tree = new BTreeMap<TKey, TValue>(_degree);
        foreach (var tracer in _tracers)
        {
            tracer.Reconstruct(tree);
        }

        return tree;
    }
}

file class BTreeTracerDebugView<TKey, TValue>(BTreeTracer<TKey, TValue> tracer) where TKey : INumber<TKey>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public List<BTreeTracer<TKey, TValue>.ITracer> Tracers => tracer._tracers;
}

file class InsertTracerProxy<TKey, TValue>(BTreeTracer<TKey, TValue>.InsertTracer tracer) where TKey : INumber<TKey>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue> Insert => new(tracer.Key, tracer.Value);
}

file class DeleteTracerProxy<TKey, TValue>(BTreeTracer<TKey, TValue>.DeleteTracer tracer) where TKey : INumber<TKey>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public TKey Key => tracer.Key;
}
