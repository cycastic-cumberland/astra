using System.Buffers;
using Astra.Collections;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.Planners;

public struct Column<T>
{
    private readonly SortedSet<int> _affected;
    private ArrayList<OperationBlueprint> _blueprints;
    private readonly int _index;

    internal Column(OperationBlueprint[] blueprints, int length, int index, SortedSet<int> affected)
    {
        _affected = affected;
        _blueprints = new(blueprints, length);
        _index = index;
    }
    
    private PhysicalPlanBuilder BinaryOperation(uint op, T value)
    {
        if (!DataCell.TryCreate(value, out var converted)) throw new DataTypeNotSupportedException();
        _affected.Add(_index);
        _blueprints.Add(new()
        {
            QueryOperationType = QueryType.FilterMask,
            Offset = _index,
            PredicateOperationType = op,
            Cell1 = converted
        });
        _blueprints.Consume(out var blueprints, out var length);
        return new(blueprints, length, _affected);
    }

    public PhysicalPlanBuilder EqualsTo(T value)
    {
        return BinaryOperation(Operation.Equal, value);
    }
    
    public PhysicalPlanBuilder NotEqualsTo(T value)
    {
        return BinaryOperation(Operation.NotEqual, value);
    }

    public PhysicalPlanBuilder Between(T lowerBound, T upperBound)
    {
        if (!DataCell.TryCreate(lowerBound, out var converted1)) throw new DataTypeNotSupportedException();
        if (!DataCell.TryCreate(upperBound, out var converted2)) throw new DataTypeNotSupportedException();
        _affected.Add(_index);
        _blueprints.Add(new()
        {
            QueryOperationType = QueryType.FilterMask,
            Offset = _index,
            PredicateOperationType = Operation.ClosedBetween,
            Cell1 = converted1,
            Cell2 = converted2
        });
        _blueprints.Consume(out var blueprints, out var length);
        return new(blueprints, length, _affected);
    }
    
    public PhysicalPlanBuilder GreaterThan(T value)
    {
        return BinaryOperation(Operation.GreaterThan, value);
    }
    
    public PhysicalPlanBuilder GreaterOrEqualsTo(T value)
    {
        return BinaryOperation(Operation.GreaterOrEqualsTo, value);
    }
    
    public PhysicalPlanBuilder LessThan(T value)
    {
        return BinaryOperation(Operation.LesserThan, value);
    }
    
    public PhysicalPlanBuilder LessThanOrEqualsTo(T value)
    {
        return BinaryOperation(Operation.LesserOrEqualsTo, value);
    }
}

public struct PhysicalPlanBuilder
{
    internal readonly SortedSet<int> Affected;
    internal ArrayList<OperationBlueprint> Blueprints;

    public static PhysicalPlanBuilder New => new(Array.Empty<OperationBlueprint>(), 0, new());
    
    internal PhysicalPlanBuilder(OperationBlueprint[] blueprints, int length, SortedSet<int> affected)
    {
        Blueprints = new(blueprints, length);
        Affected = affected;
    }

    private Column<T> GetColumn<T>(int index)
    {
        Blueprints.Consume(out var blueprints, out var length);
        return new(blueprints, length, index, Affected);
    }

    public static Column<T> Column<T>(int index) => New.GetColumn<T>(index);
    
    private PhysicalPlanBuilder Composite(uint op, PhysicalPlanBuilder other)
    {
        Blueprints.Consume(out var lhs, out var lSize);
        other.Blueprints.Consume(out var rhs, out var rSize);
        var newSize = lSize + rSize + 1;
        var newArray = ArrayPool<OperationBlueprint>.Shared.Rent(newSize);
        try
        {
            const int pad1 = 1;
            var pad2 = pad1 + lSize;
            newArray[0] = new()
            {
                QueryOperationType = op,
            };
            for (var i = 0; i < lSize; i++)
            {
                newArray[i + pad1] = lhs[i];
            }
            for (var i = 0; i < rSize; i++)
            {
                newArray[i + pad2] = rhs[i];
            }

            return new(newArray, newSize, Affected);
        }
        finally
        {
            ArrayPool<OperationBlueprint>.Shared.Return(lhs);
            ArrayPool<OperationBlueprint>.Shared.Return(rhs);
        }
    }
    
    public PhysicalPlanBuilder And(PhysicalPlanBuilder other)
    {
        return Composite(QueryType.IntersectMask, other);
    }
    
    public PhysicalPlanBuilder Or(PhysicalPlanBuilder other)
    {
        return Composite(QueryType.UnionMask, other);
    }

    public PhysicalPlan Build()
    {
        Blueprints.Consume(out var blueprints, out var length);
        return new(blueprints, length, Affected);
    }

    public void ToStream<T>(T stream, bool reversed = false) where T : IStreamWrapper
    {
        Blueprints.Consume(out var blueprints, out var length);
        try
        {
            PhysicalPlan.ToStream(new(blueprints, 0, length), stream, reversed);
        }
        finally
        {
            ArrayPool<OperationBlueprint>.Shared.Return(blueprints);
            Affected.Clear();
        }
    }
}