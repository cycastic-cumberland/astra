using System.Security.Cryptography;
using Astra.Collections;

namespace Astra.Tests.Misc;

[TestFixture]
public class SimdBytesComparisonTestFixture
{
    private static readonly Random Rng = new();
    
    [Test]
    public void CompareEquals()
    {
        var size = Rng.Next(8, 512);
        var left = new byte[size];
        var right = new byte[size];
        using var secureRandom = RandomNumberGenerator.Create();
        secureRandom.GetBytes(left);
        Array.Copy(left, right, left.Length);
        var result = BytesComparisonHelper.Equals(left, right);
        Assert.That(result, Is.True);
    }
    
    [Test]
    public void CompareNonEquals()
    {
        var size = Rng.Next(8, 512);
        var left = new byte[size];
        var right = new byte[size];
        using var secureRandom = RandomNumberGenerator.Create();
        secureRandom.GetBytes(left);
        Array.Copy(left, right, left.Length);
        var idx = Rng.Next(0, right.Length);
        right[idx] = unchecked((byte)(right[idx] + 1));
        var result = BytesComparisonHelper.Equals(left, right);
        Assert.That(result, Is.False);
    }
}