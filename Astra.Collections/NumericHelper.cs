using System.Numerics;

namespace Astra.Collections;

file static class BoundHelper<T> where T : INumber<T>
{
    private static T GetMin()
    {
        try
        {
            return (T)typeof(T).GetField("MinValue")!.GetValue(null)!;
        }
        catch
        {
            throw new InvalidOperationException($"Unsupported type {typeof(T)}");
        }
    }
    
    private static T GetMax()
    {
        try
        {
            return (T)typeof(T).GetField("MaxValue")!.GetValue(null)!;
        }
        catch
        {
            throw new InvalidOperationException($"Unsupported type {typeof(T)}");
        }
    }

    public static readonly T MinValue = GetMin();
    public static readonly T MaxValue = GetMax();
}

public static class NumericHelper
{
    public static T GetMin<T>() where T : INumber<T>
    {
        return BoundHelper<T>.MinValue;
    }

    public static T GetMax<T>() where T : INumber<T>
    {
        return BoundHelper<T>.MaxValue;
    }
}