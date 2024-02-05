using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

// Used for debugging purpose
internal static class StructuralComparer
{
    private static bool Compare<TKey, TValue>(BTreeMap<TKey, TValue>.InternalNode lhs, BTreeMap<TKey, TValue>.InternalNode rhs) where TKey : INumber<TKey>
    {
        if (lhs.ChildCount != rhs.ChildCount)
            return false;
        for (var i = 0; i < lhs.ChildCount; i++)
        {
            if (!Compare(lhs.Children[i], rhs.Children[i]))
                return false;
        }

        return true;
    }
    
    private static bool Compare<TKey, TValue>(BTreeMap<TKey, TValue>.LeafNode lhs, BTreeMap<TKey, TValue>.LeafNode rhs) where TKey : INumber<TKey>
    {
        if (lhs.KeyCount != rhs.KeyCount)
            return false;
        for (var i = 0; i < lhs.KeyCount; i++)
        {
            if (!lhs.Pairs[i].Key.Equals(rhs.Pairs[i].Key))
                return false;
        }

        return true;
    }
    
    private static bool Compare<TKey, TValue>(BTreeMap<TKey, TValue>.INode lhs, BTreeMap<TKey, TValue>.INode rhs) where TKey : INumber<TKey>
    {
        if (lhs.IsInternal && rhs.IsInternal)
            return Compare((BTreeMap<TKey, TValue>.InternalNode)lhs, (BTreeMap<TKey, TValue>.InternalNode)rhs);
        if (lhs.IsLeaf && rhs.IsLeaf)
            return Compare((BTreeMap<TKey, TValue>.LeafNode)lhs, (BTreeMap<TKey, TValue>.LeafNode)rhs);
        return false;
    }
    
    public static bool Compare<TKey, TValue>(BTreeMap<TKey, TValue> lhs, BTreeMap<TKey, TValue> rhs) where TKey : INumber<TKey>
    {
        if (lhs == rhs) return true;
        switch (lhs.Root)
        {
            case null when rhs.Root is null: return true;
            case null when rhs.Root is not null : return false;
            case not null when rhs.Root is null: return false;
        }

        var lRoot = lhs.Root!;
        var rRoot = rhs.Root!;
        return Compare(lRoot, rRoot);
    }
}