namespace Astra.Client.Aggregator;

internal static class ColumnQueryHelper
{
    public static void ThrowUnsupportedOperation(this string name)
    {
        throw new NotSupportedException($"Unsupported operation: {name}");
    }
}

public interface IAstraColumnQuery<in T>
{
    public GenericAstraQueryBranch EqualsLiteral(T literal);
    public GenericAstraQueryBranch NotEqualsLiteral(T literal);
    public GenericAstraQueryBranch Between(T fromBound, T toBound);
    public GenericAstraQueryBranch GreaterThan(T literal);
    public GenericAstraQueryBranch GreaterOrEqualsTo(T literal);
    public GenericAstraQueryBranch LesserThan(T literal);
    public GenericAstraQueryBranch LesserOrEqualsTo(T literal);
}