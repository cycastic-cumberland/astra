using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;

namespace Astra.Common.Data.Queryable;

file static class SinglePassExpressionAnalyzerHelpers
{
    public static Dictionary<string, int> GetNameToIndex<T>()
    {
        var dict = new Dictionary<string, int>();
        var idx = 0;
        foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
        {
            dict[pi.Name] = idx++;
        }

        return dict;
    }
    
    // public static Dictionary<string, Type> GetNameToType<T>()
    // {
    //     var dict = new Dictionary<string, Type>();
    //     foreach (var pi in TypeHelpers.ToAccessibleProperties<T>())
    //     {
    //         dict[pi.Name] = pi.PropertyType;
    //     }
    //
    //     return dict;
    // }
}

file static class SinglePassExpressionAnalyzerHelpers<T>
{
    public static readonly Dictionary<string, int> NameToIndex = SinglePassExpressionAnalyzerHelpers.GetNameToIndex<T>();
}

public struct SinglePassExpressionAnalyzer<T>
{
    private static readonly MethodInfo WhereMethod =
        new Func<IQueryable<T>, Expression<Func<T, bool>>, IQueryable<T>>(System.Linq.Queryable.Where).Method;
    private readonly Expression _expression;
    private readonly Stream _outStream;
    private readonly Type _queryableType;
    private bool _whereCalled;
    
    public SinglePassExpressionAnalyzer(Expression expression, Stream outStream, Type queryableType)
    {
        _expression = expression;
        _outStream = outStream;
        _queryableType = queryableType;
    }
    
    public void Analyze()
    {
        switch (_expression)
        {
            case MethodCallExpression methodCallExpression:
                Analyze(methodCallExpression);
                break;
            case UnaryExpression unaryExpression:
                Analyze(unaryExpression);
                break;
            case LambdaExpression lambdaExpression:
                Analyze(lambdaExpression);
                break;
            case BinaryExpression binaryExpression:
                Analyze(binaryExpression);
                break;
            default:
                throw new NotSupportedException(_expression.NodeType.ToString());
        }
    }

    
    private void AnalyzeUnknown(Expression expression)
    {
        switch (expression)
        {
            case UnaryExpression unaryExpression:
                AnalyzeInternal(unaryExpression);
                break;
            case LambdaExpression lambdaExpression:
                AnalyzeInternal(lambdaExpression);
                break;
            case BinaryExpression binaryExpression:
                AnalyzeInternal(binaryExpression);
                break;
            default:
                throw new NotSupportedException(expression.NodeType.ToString());
        }
    }

    private void WhereGate()
    {
        if (_whereCalled)
            throw new NotSupportedException("Does not support multiple where calls");
        _whereCalled = true;
    }
    
    private void Analyze(MethodCallExpression expression)
    {
        WhereGate();
        AnalyzeInternal(expression);
    }
    
    private void AnalyzeInternal(MethodCallExpression expression)
    {
        if (expression.Method != WhereMethod)
            throw new NotSupportedException("Unsupported method provided");
        if (expression.Arguments.Count != 2) throw new UnreachableException();
        if (expression.Arguments[0] is not ConstantExpression constantExpression ||
            constantExpression.Type != _queryableType) throw new UnreachableException();
        AnalyzeUnknown(expression.Arguments[1]);
    }
    
    private void Analyze(UnaryExpression expression)
    {
        WhereGate();
        AnalyzeInternal(expression);
    }
    
    private void AnalyzeInternal(UnaryExpression expression)
    {
        if (expression.NodeType != ExpressionType.Quote || expression.Operand is not LambdaExpression lambdaExpression)
            throw new NotSupportedException("Only supports Quote type unary expression with lambda operand");
        AnalyzeInternal(lambdaExpression);
    }
    
    private void Analyze(LambdaExpression expression)
    {
        WhereGate();
        AnalyzeInternal(expression);
    }
    
    private void AnalyzeInternal(LambdaExpression expression)
    {
        if (expression.Body is not BinaryExpression binaryExpression)
            throw new NotSupportedException("Only supports binary lambda body");
        AnalyzeInternal(binaryExpression);
    }
    
    private void Analyze(BinaryExpression expression)
    {
        WhereGate();
        AnalyzeInternal(expression);
    }
    
    private void AnalyzeInternal(BinaryExpression expression)
    {
        if (expression.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            AnalyzeMasked(expression);
            return;
        }
        switch (expression)
        {
            case { Left: MemberExpression parameterExpressionLeft, Right: ConstantExpression constantExpressionRight }:
                AnalyzeBinaryNoNesting(parameterExpressionLeft, constantExpressionRight, expression.NodeType);
                return;
            case { Right: MemberExpression parameterExpressionRight, Left: ConstantExpression constantExpressionLeft }:
                AnalyzeBinaryNoNesting(parameterExpressionRight, constantExpressionLeft, expression.NodeType);
                return;
        }

        throw new NotSupportedException();
    }

    private void AnalyzeMasked(BinaryExpression expression)
    {
        BinaryExpression binaryLeft;
        Expression genericRight;
        if (expression.Left is BinaryExpression lb)
        {
            binaryLeft = lb;
            genericRight = expression.Right;
        }
        else if (expression.Right is BinaryExpression rb)
        {
            binaryLeft = rb;
            genericRight = expression.Left;
        }
        else throw new NotSupportedException("Either operand must be binary when using masking operator");

        switch (expression.NodeType)
        {
            case ExpressionType.AndAlso:
                AnalyzeIntersect(binaryLeft, genericRight);
                break;
            case ExpressionType.OrElse:
                AnalyzeUnion(binaryLeft, genericRight);
                break;
            default:
                throw new NotSupportedException($"Unsupported masking operation: {expression.NodeType.ToString()}");
        }
    }

    private static bool IsSupportedNumericType(Type type)
    {
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return true;
            default:
                return false;
        }
    }

    private void AnalyzeBinaryNoNesting(MemberExpression parameter, ConstantExpression constant, ExpressionType nodeType)
    {
        _outStream.WriteValue(QueryType.FilterMask);
        var offset = SinglePassExpressionAnalyzerHelpers<T>.NameToIndex[parameter.Member.Name ?? throw new NullReferenceException()]; 
        _outStream.WriteValue(offset);
        switch (nodeType)
        {
            case ExpressionType.Equal:
            {
                _outStream.WriteValue(Operation.Equal);
                break;
            }
            case ExpressionType.NotEqual:
            {
                _outStream.WriteValue(Operation.NotEqual);
                break;
            }
            case ExpressionType.GreaterThan:
            {
                if (!IsSupportedNumericType(parameter.Type))
                    throw new NotSupportedException(parameter.Type.ToString());
                _outStream.WriteValue(Operation.GreaterThan);
                break;
            }
            case ExpressionType.GreaterThanOrEqual:
            {
                if (!IsSupportedNumericType(parameter.Type))
                    throw new NotSupportedException(parameter.Type.ToString());
                _outStream.WriteValue(Operation.GreaterOrEqualsTo);
                break;
            }
            case ExpressionType.LessThan:
            {
                if (!IsSupportedNumericType(parameter.Type))
                    throw new NotSupportedException(parameter.Type.ToString());
                _outStream.WriteValue(Operation.LesserThan);
                break;
            }
            case ExpressionType.LessThanOrEqual:
            {
                if (!IsSupportedNumericType(parameter.Type))
                    throw new NotSupportedException(parameter.Type.ToString());
                _outStream.WriteValue(Operation.LesserOrEqualsTo);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nodeType.ToString());
        }
        _outStream.WriteValue(DataType.DotnetTypeToAstraType(parameter.Type));
        _outStream.WriteWildcard(constant.Value);
    }

    private void ClosedBetween(MemberExpression parameter, ConstantExpression lower,
        ConstantExpression upper)
    {
        _outStream.WriteValue(QueryType.FilterMask);
        var offset = SinglePassExpressionAnalyzerHelpers<T>.NameToIndex[parameter.Member.Name]; 
        _outStream.WriteValue(offset);
        _outStream.WriteValue(Operation.ClosedBetween);
        _outStream.WriteValue(DataType.DotnetTypeToAstraType(parameter.Type));
        _outStream.WriteWildcard(lower.Value);
        _outStream.WriteWildcard(upper.Value);
    }

    private static bool IsLesserThan(object? lhs, object? rhs)
    {
        try
        {
            return Comparer.Default.Compare(lhs, rhs) < 0;
        }
        catch
        {
            return false;
        }
    }
    
    public static List<Type> GetParentClasses(Type type)
    {
        List<Type> parentClasses = new List<Type>();

        // Get the base type of the given type
        Type baseType = type.BaseType;

        // Recursively get parent classes until reaching the top-level base class (object)
        while (baseType != null && baseType != typeof(object))
        {
            parentClasses.Add(baseType);
            baseType = baseType.BaseType;
        }

        return parentClasses;
    }
    
    private bool TryRanged(BinaryExpression binaryLeft, Expression rightExpression)
    {
        if (rightExpression is not BinaryExpression binaryRight) return false;
        
        // (constant1 <= x) && (x <= constant2)
        if (binaryLeft.NodeType == ExpressionType.LessThanOrEqual &&
            binaryRight.NodeType == ExpressionType.LessThanOrEqual)
        {
            if (binaryLeft is not { Left: ConstantExpression constLower, Right: MemberExpression param1 } ||
                binaryRight is not
                    { Left: MemberExpression param2, Right: ConstantExpression constUpper }) return false;
            if (param1.Member != param2.Member) return false;
            if (!IsLesserThan(constLower.Value, constUpper.Value)) return false;
            ClosedBetween(param1, constLower, constUpper);
            return true;

        }
        // (x >= constant1) && (x <= constant2)
        if (binaryLeft.NodeType == ExpressionType.GreaterThanOrEqual &&
            binaryRight.NodeType == ExpressionType.LessThanOrEqual)
        {
            if (binaryLeft is not { Right: ConstantExpression constLower, Left: MemberExpression param1 } ||
                binaryRight is not
                    { Left: MemberExpression param2, Right: ConstantExpression constUpper }) return false;
            if (param1.Member != param2.Member) return false;
            if (!IsLesserThan(constLower.Value, constUpper.Value)) return false;
            ClosedBetween(param1, constLower, constUpper);
            return true;

        }
        // (constant1 <= x) && (constant2 >= x)
        if (binaryLeft.NodeType == ExpressionType.LessThanOrEqual &&
            binaryRight.NodeType == ExpressionType.GreaterThanOrEqual)
        {
            if (binaryLeft is not { Right: ConstantExpression constLower, Left: MemberExpression param1 } ||
                binaryRight is not
                    { Right: MemberExpression param2, Left: ConstantExpression constUpper }) return false;
            if (param1.Member != param2.Member) return false;
            if (!IsLesserThan(constLower.Value, constUpper.Value)) return false;
            ClosedBetween(param1, constLower, constUpper);
            return true;

        }
        // (x >= constant1) && (constant2 >= x)
        if (binaryLeft.NodeType == ExpressionType.GreaterThanOrEqual &&
            binaryRight.NodeType == ExpressionType.GreaterThanOrEqual)
        {
            if (binaryLeft is not { Left: ConstantExpression constLower, Right: MemberExpression param1 } ||
                binaryRight is not
                    { Right: MemberExpression param2, Left: ConstantExpression constUpper }) return false;
            if (param1.Member != param2.Member) return false;
            if (!IsLesserThan(constLower.Value, constUpper.Value)) return false;
            ClosedBetween(param1, constLower, constUpper);
            return true;

        }

        return false;
    }
    
    private void AnalyzeIntersect(BinaryExpression binaryExpression, Expression rightExpression)
    {
        // Could be ranged
        if (TryRanged(binaryExpression, rightExpression)) return;
        _outStream.WriteValue(QueryType.IntersectMask);
        AnalyzeInternal(binaryExpression);
        AnalyzeUnknown(rightExpression);
    }
    
    private void AnalyzeUnion(BinaryExpression binaryExpression, Expression rightExpression)
    {
        _outStream.WriteValue(QueryType.UnionMask);
        AnalyzeInternal(binaryExpression);
        AnalyzeUnknown(rightExpression);
    }
}