using System.Collections.Immutable;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;

namespace Astra.Tests.BTree;

public class DoubleBTreeMapTestFixture
{
    private const int RepeatCount = 50;
    private Random _rng  =null!;
    private BTreeMap<double, double> _tree = null!;
    private Dictionary<double, double> _correspondingDict = null!;
#if DEBUG
    private BTreeTracer<double, double> _tracer = null!;
#endif    

    private double ClampedRandom => _rng.Next(double.MinValue / 2.0, double.MaxValue / 2.0);
    
    [SetUp]
    public void SetUp()
    {
        _rng = new();
        _tree = new(_rng.Next(BTreeMap.MinDegree, 20));
        _correspondingDict = new();
#if DEBUG
        _tracer = new(_tree.Degree);
#endif
    }
    
    private void InsertRandom(int iterationCount)
    {
        for (var i = 0; i < iterationCount; i++)
        {
            var key = ClampedRandom;
            var value = ClampedRandom;
            _correspondingDict[key] = value;
            _tree[key] = value;
#if DEBUG
            _tracer[key] = value;
#endif
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

    private void AssertSortedDictionary(ImmutableSortedDictionary<double, double> d1,
        IEnumerable<KeyValuePair<double, double>> query)
    {
        var d2 = new SortedDictionary<double, double>();
        var last = double.MinValue;
        recheck:
        foreach (var (k, v) in query)
        {
            Assert.That(k, Is.GreaterThanOrEqualTo(last));
            last = k;
            d2[k] = v;
        }
        
#if DEBUG
        if (d2.Count != d1.Count)
        {
            {
                var newTree = _tracer.Reconstruct();
                _ = _tree.StructuralCompare(newTree);
                query = newTree.CollectTo(double.MinValue);
            }
            goto recheck;
        }
#endif
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
        double left, right;
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
        var targetKeys = new HashSet<double>();
        var keyList = _correspondingDict.Keys.ToArray();
        for (var i = 0; i < removalAmount; i++)
        {
            double key;
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
#if DEBUG
            _tracer.Remove(key);
#endif
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
        _tree[bound] = 42;
        _correspondingDict[bound] = 42;
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
        _tree[bound] = 42;
        _correspondingDict[bound] = 42;
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
        _tree[bound] = 42;
        _correspondingDict[bound] = 42;
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
        _tree[bound] = 42;
        _correspondingDict[bound] = 42;
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
        _tracer.Clear();
        Assert.That(_tree, Is.Empty);
        Assert.That(_tree, Has.Count.EqualTo(_correspondingDict.Count));
    }

    [Test, Repeat(RepeatCount)]
    public void ClearTest()
    {
        InsertRandom();
        ClearTestInternal();
    }

    [Test, Repeat(short.MaxValue)]
    public void CombinedTest()
    {
        InsertRandom();
        PointQueryTestInternal();
        RangeQueryTestInternal();
        GreaterTestInternal();
        GreaterOrEqualTestInternal();
        LowerTestInternal();
        LowerOrEqualTestInternal();
        
        // RemovalTestInternal();
        
        PointQueryTestInternal();
        RangeQueryTestInternal();
        GreaterTestInternal();
        GreaterOrEqualTestInternal();
        LowerTestInternal();
        LowerOrEqualTestInternal();
        ClearTestInternal();
    }
}