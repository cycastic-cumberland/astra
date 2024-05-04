using System.Buffers;
using Astra.Collections;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.Planners;

public readonly struct PhysicalPlan : IDisposable
{
    private readonly SortedSet<int> _affectedColumns;
    private readonly OperationBlueprint[] _blueprints;
    private readonly int _blueprintLength;

    public IReadOnlySet<int> AffectedColumns => _affectedColumns;
    public ReadOnlySpan<OperationBlueprint> Blueprints => new(_blueprints, 0, _blueprintLength);

    public PhysicalPlan(OperationBlueprint[] blueprints, int blueprintLength, SortedSet<int> affected)
    {
        _affectedColumns = affected;
        _blueprints = blueprints;
        _blueprintLength = blueprintLength;
    }
    internal static void ResolveFilterOperation(ref OperationBlueprint blueprint, SortedSet<int> affected, 
        ColumnSchema[] tableSchema, Stream query)
    {
        blueprint.QueryOperationType = QueryType.FilterMask;
        var offset = query.ReadInt();
        var schema = tableSchema[offset];
        if (!schema.IsIndex)
        {
            blueprint.Offset = -1;
            return;
        }

        affected.Add(offset);
        blueprint.Offset = offset;
        var operation = query.ReadUInt();
        blueprint.PredicateOperationType = operation;
        switch (operation)
        {
            case Operation.ClosedBetween:
            {
                query.CheckDataType(schema.Type);
                blueprint.Cell1 = DataCell.FromStream(schema.Type.Value, query);
                blueprint.Cell2 = DataCell.FromStream(schema.Type.Value, query);
                break;
            }
            case Operation.GreaterThan:
            case Operation.GreaterOrEqualsTo:
            case Operation.LesserThan:
            case Operation.LesserOrEqualsTo:
            {
                if (!schema.Type.IsNumeric)
                    throw new OperationNotSupported($"Filter operation not supported for type {schema.Type.Value}");
                goto case Operation.Equal;
            }
            case Operation.Equal:
            {
                query.CheckDataType(schema.Type);
                var cond = DataCell.FromStream(schema.Type.Value, query);
                blueprint.Cell1 = cond;
                break;
            }
            default:
            {
                throw new OperationNotSupported($"Operation not supported: {operation}");
            }
        }
    }
    
    public static PhysicalPlan CreatePlan(ColumnSchema[] tableSchema, Stream query)
    {
        var affected = new SortedSet<int>();
        using var blueprintList = new ArrayList<OperationBlueprint>();
        var stuff = 1;
        while (stuff > 0)
        {
            var type = query.ReadUInt();
            switch (type)
            {
                // case QueryType.EndOfQuery:
                //     goto outOfLoop;
                case QueryType.IntersectMask:
                {
                    stuff += 2;
                    blueprintList.Add(new OperationBlueprint
                    {
                        QueryOperationType = QueryType.IntersectMask
                    });
                    break;
                }
                case QueryType.UnionMask:
                {
                    stuff += 2;
                    blueprintList.Add(new OperationBlueprint
                    {
                        QueryOperationType = QueryType.UnionMask
                    });
                    break;
                }
                case QueryType.FilterMask:
                {
                    stuff -= 1;
                    var blueprint = new OperationBlueprint();
                    ResolveFilterOperation(ref blueprint, affected, tableSchema, query);
                    blueprintList.Add(blueprint);
                    break;
                }
                default:
                    throw new AggregateException($"Operation type type not supported: {type}");
            }
        }

        // outOfLoop: 
        blueprintList.Consume(out var blueprints, out var blueprintLength);
        return new(blueprints, blueprintLength, affected);
    }

    public void ToStream<T>(T stream) where T : IStreamWrapper => ToStream(Blueprints, stream);
    
    public static void ToStream<T>(ReadOnlySpan<OperationBlueprint> blueprints, T stream) where T : IStreamWrapper
    {
        for (var i = 0; i < blueprints.Length; i++)
        {
            ref readonly var blueprint = ref blueprints[i];
            OperationBlueprint.ToStream(in blueprint, ref stream);
        }
        // stream.SaveValue(QueryType.EndOfQuery);
    }

    public void Dispose()
    {
        ArrayPool<OperationBlueprint>.Shared.Return(_blueprints);
    }
}