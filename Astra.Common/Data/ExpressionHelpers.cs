using System.Diagnostics;
using System.Linq.Expressions;

namespace Astra.Common.Data;

public static class ExpressionHelpers
{
    public static T GetConstant<T>(this BinaryExpression binaryExpression)
    {
        if (binaryExpression.Left is ConstantExpression cl)
        {
            if (cl.Type != typeof(T)) throw new Exception("Mismatched data type");
            return (T)(cl.Value ?? throw new UnreachableException());
        }
        if (binaryExpression.Right is ConstantExpression cr)
        {
            if (cr.Type != typeof(T)) throw new Exception("Mismatched data type");
            return (T)(cr.Value ?? throw new UnreachableException());
        }

        throw new Exception("No constant found");
    }
}