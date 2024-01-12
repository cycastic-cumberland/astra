using Astra.Common;

namespace Astra.Client.Aggregator;

public class IntegerColumn(int offset) : IAstraColumnQuery<int>
{
    public GenericAstraQueryBranch EqualsLiteral(int literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(int literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}