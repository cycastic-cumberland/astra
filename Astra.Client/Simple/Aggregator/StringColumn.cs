using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;

namespace Astra.Client.Simple.Aggregator;

public class StringColumn(int offset) : IAstraColumnQuery<string>
{
    public GenericAstraQueryBranch EqualsLiteral(string literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(DataType.StringMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(string literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(PredicateType.UnaryMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(DataType.StringMask);
        stream.WriteValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch Between(string fromBound, string toBound)
    {
        nameof(Between).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch GreaterThan(string literal)
    {
        nameof(GreaterThan).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch GreaterOrEqualsTo(string literal)
    {
        nameof(GreaterOrEqualsTo).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch LesserThan(string literal)
    {
        nameof(LesserThan).ThrowUnsupportedOperation();
        return new();
    }

    public GenericAstraQueryBranch LesserOrEqualsTo(string literal)
    {
        nameof(LesserOrEqualsTo).ThrowUnsupportedOperation();
        return new();
    }
}