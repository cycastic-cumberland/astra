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

    public GenericAstraQueryBranch Between(int lowerBound, int upperBound)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.ClosedBetween);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(lowerBound);
        stream.WriteValue(upperBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch GreaterThan(int fromBound)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.GreaterThan);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch GreaterOrEqualsTo(int fromBound)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.GreaterOrEqualsTo);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch LesserThan(int fromBound)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.LesserThan);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch LesserOrEqualsTo(int toBound)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.LesserOrEqualsTo);
        stream.WriteValue(DataType.DWordMask);
        stream.WriteValue(toBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}