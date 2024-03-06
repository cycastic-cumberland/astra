using Astra.Common;

namespace Astra.Client.Simple.Aggregator;

public class BytesColumn(int offset) : IAstraColumnQuery<byte[]>
{
    public GenericAstraQueryBranch EqualsLiteral(byte[] literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(DataType.BytesMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(byte[] literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(DataType.BytesMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch Between(byte[] fromBound, byte[] toBound)
    {
        nameof(Between).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch GreaterThan(byte[] literal)
    {
        nameof(GreaterThan).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch GreaterOrEqualsTo(byte[] literal)
    {
        nameof(GreaterOrEqualsTo).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch LesserThan(byte[] literal)
    {
        nameof(LesserThan).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch LesserOrEqualsTo(byte[] literal)
    {
        nameof(LesserOrEqualsTo).ThrowUnsupportedOperation();
        return new();
    }
}