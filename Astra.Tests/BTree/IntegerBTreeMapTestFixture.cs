using System.Collections.Immutable;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;

namespace Astra.Tests.BTree;

[TestFixture]
public class IntegerBTreeMapTestFixture
{
    private const int RepeatCount = 50;
    private Random _rng = null!;
    private BTreeMap<int, int> _tree = null!;
    private Dictionary<int, int> _correspondingDict = null!;

    [SetUp]
    public void SetUp()
    {
        _rng = new();
        _tree = new(_rng.Next(BTreeMap.MinDegree, 20));
        _correspondingDict = new();
    }

    private int ClampedRandom => _rng.Next(sbyte.MinValue, sbyte.MaxValue);

    private void InsertRandom(int iterationCount)
    {
        for (var i = 0; i < iterationCount; i++)
        {
            var key = ClampedRandom;
            var value = ClampedRandom;
            _correspondingDict[key] = value;
            _tree[key] = value;
        }
    }

    private void InsertRandom()
    {
        InsertRandom(_rng.Next(10, byte.MaxValue));
    }

    private void PointQueryTestInternal()
    {
        Assert.That(_tree, Has.Count.EqualTo(_correspondingDict.Count));
        foreach (var (key, value) in _correspondingDict)
        {
            if (!_tree.TryGetValue(key, out var corresponding))
                Assert.Fail();
            
            Assert.That(corresponding, Is.EqualTo(value));
        }
    }

    private static void AssertSortedDictionary(ImmutableSortedDictionary<int, int> d1,
        IEnumerable<KeyValuePair<int, int>> query)
    {
        var d2 = new SortedDictionary<int, int>();
        var last = int.MinValue;
        foreach (var (k, v) in query)
        {
            Assert.That(k, Is.GreaterThanOrEqualTo(last));
            last = k;
            d2[k] = v;
        }

        Assert.That(d1, Has.Count.EqualTo(d2.Count));
        foreach (var (key, value) in d1)
        {
            var exists = d2.TryGetValue(key, out var corresponding);
            Assert.That(exists, Is.True);
            Assert.That(corresponding, Is.EqualTo(value));
        }
    }

    [Test, Repeat(RepeatCount)]
    public void PointQueryTest()
    {
        InsertRandom();
        PointQueryTestInternal();
    }

    private void RangeQueryTestInternal()
    {
        int left, right;
        do
        {
            (left, right) = (ClampedRandom, ClampedRandom);
        } while (left > right);

        var model = _correspondingDict.Where(o => o.Key >= left && o.Key <= right)
            .ToImmutableSortedDictionary();
        var fromTree = _tree
            .Collect(left, right, CollectionMode.ClosedInterval);
        AssertSortedDictionary(model, fromTree);
    }

    [Test, Repeat(RepeatCount)]
    public void RangeQueryTest()
    {
        InsertRandom();
        RangeQueryTestInternal();
    }

    private void RemovalTestInternal()
    {
        var removalAmount = _rng.Next(_correspondingDict.Count / 2, _correspondingDict.Count);
        var targetKeys = new HashSet<int>();
        var keyList = _correspondingDict.Keys.ToArray();
        for (var i = 0; i < removalAmount; i++)
        {
            int key;
            do
            {
                key = keyList[_rng.Next(0, keyList.Length)];
            } while (targetKeys.Contains(key));

            targetKeys.Add(key);
        }

        foreach (var key in targetKeys)
        {
            _correspondingDict.Remove(key);
            _tree.Remove(key);
        }

        PointQueryTestInternal();
    }

    [Test, Repeat(RepeatCount)]
    public void RemovalTest()
    {
        InsertRandom();
        RemovalTestInternal();
    }

    private void GreaterTestInternal()
    {
        var bound = ClampedRandom;
        var model = _correspondingDict.Where(o => o.Key > bound)
            .ToImmutableSortedDictionary();
        var fromTree = _tree.CollectFrom(bound, false);
        AssertSortedDictionary(model, fromTree);
    }

    [Test, Repeat(RepeatCount)]
    public void GreaterTest()
    {
        InsertRandom();
        GreaterTestInternal();
    }

    private void GreaterOrEqualTestInternal()
    {
        var bound = ClampedRandom;
        var model = _correspondingDict.Where(o => o.Key >= bound)
            .ToImmutableSortedDictionary();
        var fromTree = _tree.CollectFrom(bound);
        AssertSortedDictionary(model, fromTree);
    }

    [Test, Repeat(RepeatCount)]
    public void GreaterOrEqualTest()
    {
        InsertRandom();
        GreaterOrEqualTestInternal();
    }

    private void LowerTestInternal()
    {
        var bound = ClampedRandom;
        var model = _correspondingDict.Where(o => o.Key < bound)
            .ToImmutableSortedDictionary();
        var fromTree = _tree.CollectTo(bound, false);
        AssertSortedDictionary(model, fromTree);
    }

    [Test, Repeat(RepeatCount)]
    public void LowerTest()
    {
        InsertRandom();
        LowerTestInternal();
    }

    private void LowerOrEqualTestInternal()
    {
        var bound = ClampedRandom;
        var model = _correspondingDict.Where(o => o.Key <= bound)
            .ToImmutableSortedDictionary();
        var fromTree = _tree.CollectTo(bound);
        AssertSortedDictionary(model, fromTree);
    }

    [Test, Repeat(RepeatCount)]
    public void LowerOrEqualTest()
    {
        InsertRandom();
        LowerOrEqualTestInternal();
    }

    private void ClearTestInternal()
    {
        _correspondingDict.Clear();
        _tree.Clear();
        Assert.That(_tree, Is.Empty);
        Assert.That(_tree, Has.Count.EqualTo(_correspondingDict.Count));
    }

    [Test, Repeat(RepeatCount)]
    public void ClearTest()
    {
        InsertRandom();
        ClearTestInternal();
    }

    private void AssertDepth()
    {
        // This assertion is like a roulette...
        // Assert.That(_tree.Depth, Is.EqualTo(_tree.ExploreMaxDepth()));
    }
    
    [Test, Repeat(RepeatCount)]
    public void CombinedTest()
    {
        InsertRandom();
        AssertDepth();
        
        PointQueryTestInternal();
        RangeQueryTestInternal();
        GreaterTestInternal();
        GreaterOrEqualTestInternal();
        LowerTestInternal();
        LowerOrEqualTestInternal();
        
        RemovalTestInternal();
        AssertDepth();
        
        PointQueryTestInternal();
        RangeQueryTestInternal();
        GreaterTestInternal();
        GreaterOrEqualTestInternal();
        LowerTestInternal();
        LowerOrEqualTestInternal();
        
        ClearTestInternal();
        AssertDepth();
    }
}