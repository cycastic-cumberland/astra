using System.Numerics;
using System.Runtime.CompilerServices;

namespace Astra.Collections;

file static class EpsilonHelper<T> where T : INumber<T>
{
    private static readonly Type GenericType = typeof(T).IsPrimitive ? typeof(T) : throw new NotSupportedException();
    private static unsafe T EpsilonInternal
    {
        get
        {
            var sample = Activator.CreateInstance<T>();
            if (T.IsInteger(sample))
            {
                return T.One;
            }
            if (GenericType == typeof(float))
            {
                var epsilon = float.Epsilon;
                return Unsafe.Read<T>(&epsilon);
            }

            if (GenericType == typeof(double))
            {
                var epsilon = double.Epsilon;
                return Unsafe.Read<T>(&epsilon);
            }

            if (GenericType == typeof(Half))
            {
                var epsilon = Half.Epsilon;
                return Unsafe.Read<T>(&epsilon);
            }

            throw new NotSupportedException();
        }
    }

    public static readonly T Epsilon = EpsilonInternal;
    
}

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
    public static T GetEpsilon<T>() where T : INumber<T> => EpsilonHelper<T>.Epsilon;

    public static T GetMin<T>() where T : INumber<T>
    {
        return BoundHelper<T>.MinValue;
    }

    public static T GetMax<T>() where T : INumber<T>
    {
        return BoundHelper<T>.MaxValue;
    }
}