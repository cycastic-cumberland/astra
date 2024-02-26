using Astra.Collections.WideDictionary;

namespace Astra.Tests.HashMap;

[TestFixture]
public class StaticHashMapTestFixture
{
    private const int RepeatCount = 40;
    private static readonly Random Rng = new();
    private static int NextNumber => Rng.Next(int.MinValue, int.MaxValue);

    private StaticHashMap<ulong, int> _hashMap = null!;
    private Dictionary<ulong, int> _dictionary = null!;

    [Test]
    public void ControlledCollidedTest()
    {
        _hashMap = new(2);
        _hashMap[0] = 1;
        _hashMap[4] = 2;
        _hashMap[2] = 3;
        _hashMap[0] = 4;
        Assert.That(_hashMap, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(_hashMap[0], Is.EqualTo(4));
            Assert.That(_hashMap[4], Is.EqualTo(2));
            Assert.That(_hashMap[2], Is.EqualTo(3));
        });
    }

    private void IntegrityCheck()
    {
        Assert.That(_hashMap, Has.Count.EqualTo(_dictionary.Count));
        var i = 0;
        foreach (var (key, value) in _dictionary)
        {
            if (_hashMap.TryGetValue(key, out var corresponding))
            {
                Assert.That(value, Is.EqualTo(corresponding));
                i++;
                continue;
            }
            Assert.Fail();
        }
        Assert.That(i, Is.EqualTo(_dictionary.Count));
        i = 0;
        foreach (var (key, value) in _hashMap)
        {
            if (_dictionary.TryGetValue(key, out var corresponding))
            {
                Assert.That(value, Is.EqualTo(corresponding));
                i++;
                continue;
            }
            Assert.Fail();
        }
        Assert.That(i, Is.EqualTo(_dictionary.Count));
    }
    
    private void RandomInsertionTestInternal(int count)
    {
        _hashMap = new(4);
        _dictionary = new();
        for (var i = 0; i < count; i++)
        {
            var key = unchecked((ulong)NextNumber);
            var value = NextNumber;
            _hashMap[key] = value;
            _dictionary[key] = value;
        }
        
        IntegrityCheck();
    }
    
    [Test, Repeat(RepeatCount)]
    public void RandomInsertionTest()
    {
        var count = Rng.Next(10, 100);
        RandomInsertionTestInternal(count);
    }

    private void RandomRemovalTestInternal(int count)
    {
        var keysSet = new HashSet<ulong>();
        var keysCollection = _dictionary.Keys.ToArray();
        for (var i = 0; i < count; i++)
        {
            ulong key;
            do
            {
                key = keysCollection[Rng.Next(0, keysCollection.Length)];
            } while (keysSet.Contains(key));

            keysSet.Add(key);
        }

        foreach (var key in keysSet)
        {
            _hashMap.Remove(key);
            _dictionary.Remove(key);
        }

        IntegrityCheck();
    }
    
    [Test, Repeat(RepeatCount)]
    public void RandomRemovalTest()
    {
        var insertionAmount = Rng.Next(10, 100);
        var removalAmount = Rng.Next(8, insertionAmount - 1);
        RandomInsertionTestInternal(insertionAmount);
        RandomRemovalTestInternal(removalAmount);
    }
}