using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class IntegerBTreePointQueryBenchmark
{
    private static readonly Random Rng = new();
    
    private static int NextNumber => Rng.Next(int.MinValue, int.MaxValue);
    
    private Collections.RangeDictionaries.BTree.BTreeMap<int, int> _tree = null!;
    private SortedDictionary<int, int> _reference = null!;
    private int[] _pointQueryKeys = null!;

    [Params(10, 100, 1_000)]
    public int Degree;
    
    [Params(10_000, 100_000)]
    public int InsertionAmount;
    
    [Params(10_000, 100_000)]
    public int RepeatCount;

    [IterationSetup]
    public void SetUp()
    {
        _tree = new(Degree);
        _reference = new();
        _pointQueryKeys = new int[RepeatCount];
        var keys = new HashSet<int>();
        for (var i = 0; i < InsertionAmount; i++)
        {
            var key = NextNumber;
            keys.Add(key);
            _tree[key] = 42;
            _reference[key] = 42;
        }

        var arrKeys = keys.ToArray();

        for (var i = 0; i < RepeatCount; i++)
        {
            _pointQueryKeys[i] = arrKeys[Rng.Next(0, arrKeys.Length)];
        }
    }

    [Benchmark]
    public void BTreePoint()
    {
        foreach (var key in _pointQueryKeys)
        {
            var a = _tree[key];
            _ = a;
        }
    }

    [Benchmark]
    public void ReferencePoint()
    {
        foreach (var key in _pointQueryKeys)
        {
            var a = _reference[key];
            _ = a;
        }
    }
}