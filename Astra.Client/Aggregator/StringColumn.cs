using Astra.Engine;

namespace Astra.Client.Aggregator;

public class StringColumn(int offset) : IAstraColumnQuery<string>
{
    public GenericAstraQueryBranch EqualsLiteral(string literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(DataType.StringMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(string literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(DataType.StringMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}