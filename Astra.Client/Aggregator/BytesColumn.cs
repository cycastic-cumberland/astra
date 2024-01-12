using Astra.Common;

namespace Astra.Client.Aggregator;

public class BytesColumn(int offset) : IAstraColumnQuery<byte[]>
{
    public GenericAstraQueryBranch EqualsLiteral(byte[] literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(DataType.BytesMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(byte[] literal)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(DataType.BytesMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}