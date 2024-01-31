using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

// Debugging tools
public static class TraversalHelper
{
    public static void Visualize<TKey, TValue>(this RangeDictionaries.BTree.BTreeMap<TKey, TValue> map) where TKey : INumber<TKey>
    {
        map.Traverse(new ConsoleView<TKey, TValue>());
    }

    public static int ExploreMaxDepth<TKey, TValue>(this RangeDictionaries.BTree.BTreeMap<TKey, TValue> map) where TKey : INumber<TKey>
    {
        var explorer = new MaxDepthExplorer<TKey, TValue>();
        map.Traverse(explorer);
        return explorer.MaxDepth;
    }
}