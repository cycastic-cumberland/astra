using Astra.Collections;
using Astra.Collections.RangeDictionaries.BTree;

namespace TestStuff;

public static class Program
{
    public static void Main(string[] args)
    {
        var rng = new Random();
        var tree = new Astra.Collections.RangeDictionaries.BTree.BTreeMap<long, int>(5);
        for (var i = 0; i < 200; i++)
        {
            var j = rng.NextInt64(long.MinValue, long.MaxValue);
            tree[j] = i;
        }
        
        tree.Visualize();
    }
}