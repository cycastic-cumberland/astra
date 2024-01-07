namespace Astra.Client.Aggregator;

public interface IAstraColumnQuery<in T>
{
    public GenericAstraQueryBranch EqualsLiteral(T literal);
    public GenericAstraQueryBranch NotEqualsLiteral(T literal);
}