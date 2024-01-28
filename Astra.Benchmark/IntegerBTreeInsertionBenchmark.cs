using Astra.Collections.RangeDictionaries.BTree;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class IntegerBTreeInsertionBenchmark
{
    private static readonly Random Rng = new();

    private static int NextNumber => Rng.Next(int.MinValue, int.MaxValue);
    
    private BTreeMap<int, int> _tree = null!;
    private SortedDictionary<int, int> _reference = null!;

    [Params(10, 100)]
    public int Degree;

    [Params(1_000, 10_000, 100_000)]
    public int InsertionAmount;

    [IterationSetup]
    public void SetUp()
    {
        _tree = new(Degree);
        _reference = new();
    }

    [Benchmark]
    public void BTree()
    {
        for (var i = 0; i < InsertionAmount; i++)
        {
            _tree[NextNumber] = 42;
        }
    }

    [Benchmark]
    public void Reference()
    {
        for (var i = 0; i < InsertionAmount; i++)
        {
            _reference[NextNumber] = 42;
        }
    }
}