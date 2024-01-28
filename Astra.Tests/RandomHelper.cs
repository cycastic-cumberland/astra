namespace Astra.Tests;

internal static class RandomHelper
{
    public static double Next(this Random rng, double fromInclusive, double toExclusive)
    {
        Assert.Multiple(() =>
        {
            Assert.That(double.IsInfinity(fromInclusive), Is.False);
            Assert.That(double.IsNegativeInfinity(fromInclusive), Is.False);
            Assert.That(double.IsInfinity(toExclusive), Is.False);
            Assert.That(double.IsNegativeInfinity(toExclusive), Is.False);
            Assert.That(fromInclusive, Is.LessThan(toExclusive));
        });
        var range = toExclusive - fromInclusive;

        double ret;
        do
        {
            var scaledNumber = rng.NextDouble() * range;
            ret = fromInclusive + scaledNumber;
        } while (double.IsInfinity(ret) || double.IsNegativeInfinity(ret));

        return ret;
    }

    public static float Next(this Random rng, float fromInclusive, float toExclusive)
    {
        Assert.Multiple(() =>
        {
            Assert.That(float.IsInfinity(fromInclusive), Is.False);
            Assert.That(float.IsNegativeInfinity(fromInclusive), Is.False);
            Assert.That(float.IsInfinity(toExclusive), Is.False);
            Assert.That(float.IsNegativeInfinity(toExclusive), Is.False);
            Assert.That(fromInclusive, Is.LessThan(toExclusive));
        });
        var range = toExclusive - fromInclusive;
        
        float ret;
        do
        {
            var scaledNumber = rng.NextSingle() * range;
            ret = fromInclusive + scaledNumber;
        } while (float.IsInfinity(ret) || float.IsNegativeInfinity(ret));

        return ret;
    }
}