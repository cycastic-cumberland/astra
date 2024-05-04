using System.Numerics;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;

namespace Astra.Client.Simple.Aggregator;

public abstract class NumericColumn<T>(int offset, uint mask) : IAstraColumnQuery<T>
    where T : unmanaged, INumber<T>
{
    public GenericAstraQueryBranch EqualsLiteral(T literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.Equal);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }

    public GenericAstraQueryBranch NotEqualsLiteral(T literal)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.NotEqual);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(literal);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch Between(T lowerBound, T upperBound)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.ClosedBetween);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(lowerBound);
        stream.WriteUnmanagedValue(upperBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch GreaterThan(T fromBound)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.GreaterThan);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch GreaterOrEqualsTo(T fromBound)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.GreaterOrEqualsTo);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch LesserThan(T fromBound)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.LesserThan);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(fromBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch LesserOrEqualsTo(T toBound)
    {
        using var wrapped = LocalStreamWrapper.Create();
        var stream = wrapped.LocalStream;
        stream.WriteValue(QueryType.FilterMask);
        stream.WriteValue(offset);
        stream.WriteValue(Operation.LesserOrEqualsTo);
        stream.WriteValue(mask);
        stream.WriteUnmanagedValue(toBound);
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}

public sealed class IntegerColumn(int offset) : NumericColumn<int>(offset, DataType.DWordMask);
public sealed class LongColumn(int offset) : NumericColumn<long>(offset, DataType.QWordMask);
public sealed class SingleColumn(int offset) : NumericColumn<float>(offset, DataType.SingleMask);
public sealed class DoubleColumn(int offset) : NumericColumn<double>(offset, DataType.DoubleMask);