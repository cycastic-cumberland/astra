using Astra.Collections.RangeDictionaries.BTree;
using BenchmarkDotNet.Attributes;

namespace Astra.Benchmark;

public class IntegerBTreeRangeQueryBenchmark
{
    private static readonly Random Rng = new();
    
    private static int NextNumber => Rng.Next(int.MinValue, int.MaxValue);
    
    private BTreeMap<int, int> _tree = null!;
    private int _lower;
    private int _upper;
    
    [Params(10, 100, 1_000)]
    public int Degree;
    
    [Params(10_000, 100_000)]
    public int InsertionAmount;
    
    [Params(1_000, 5_000, 8_000)]
    public int FetchAmount;

    [Params(100, 1_000)]
    public int RepeatCount;

    private int HalfFetch => FetchAmount / 2;
    private int MaxShiftAmount => (InsertionAmount - FetchAmount) / 2 - 4;
    
    [IterationSetup]
    public void SetUp()
    {
        _tree = new(Degree);
        var keys = new HashSet<int>();
        for (var i = 0; i < InsertionAmount; i++)
        {
            int key;
            do
            {
                key = NextNumber;
            } while (keys.Contains(key));

            keys.Add(key);
            _tree[key] = 42;
        }

        var sortedKeys = _tree.Keys.ToArray();
        var halfSize = InsertionAmount / 2;

        var lowerIdx = halfSize - HalfFetch;
        var upperIdx = halfSize + HalfFetch;
        var shiftAmount = Rng.Next(-MaxShiftAmount, MaxShiftAmount);
        lowerIdx += shiftAmount;
        upperIdx += shiftAmount;
        _lower = sortedKeys[lowerIdx];
        _upper = sortedKeys[upperIdx];
    }
    
    [Benchmark]
    public void BTreeRangeQuery()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            var range = _tree.CollectBetween(_lower, _upper);
            foreach (var v in range)
            {
                _ = v;
            }
        }
    }
}