using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

internal class BTreeDebugView<TKey, TValue>(BTreeMap<TKey, TValue> tree) where TKey : INumber<TKey>
{
    public KeyValuePair<TKey, TValue>[] Pairs => tree.ToArray();
    public BTreeMap<TKey, TValue>.INode? Root => tree.Root;
    public long Count => tree.LongCount;
    public int Depth => tree.Depth;
    public int Degree => tree.Degree;
}