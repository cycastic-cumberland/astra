using System.Collections;
using Astra.Common.Data;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.Planners.Physical;

public struct ReversedPhysicalPlanReader : IEnumerator<OperationBlueprint>
{
    private readonly Stream _query;
    private readonly SortedSet<int> _affected = new();
    private readonly ColumnSchema[] _tableSchema;
    private OperationBlueprint _blueprint;
    private bool _ended;

    public ReversedPhysicalPlanReader(Stream query, ColumnSchema[] tableSchema)
    {
        _query = query;
        _tableSchema = tableSchema;
    }

    public bool MoveNext()
    {
        if (_ended) return false;
        var type = _query.ReadUInt();
        switch (type)
        {
            case QueryType.EndOfQuery:
            {
                _ended = true;
                return false;
            }
            case QueryType.IntersectMask:
            {
                _blueprint = new OperationBlueprint
                {
                    QueryOperationType = QueryType.IntersectMask
                };
                break;
            }
            case QueryType.UnionMask:
            {
                _blueprint = new OperationBlueprint
                {
                    QueryOperationType = QueryType.UnionMask
                };
                break;
            }
            case QueryType.FilterMask:
            {
                _blueprint = new OperationBlueprint();
                PhysicalPlan.ResolveFilterOperation(ref _blueprint, _affected, _tableSchema, _query);
                break;
            }
            default:
                throw new AggregateException($"Operation type type not supported: {type}");
        }

        return true;
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    public OperationBlueprint Current => _blueprint;

    object IEnumerator.Current => Current;

    public void Dispose()
    {
        
    }
}

public readonly struct ReversedPhysicalPlanEnumerable(Stream query, ColumnSchema[] tableSchema) : IEnumerable<OperationBlueprint>
{
    public ReversedPhysicalPlanReader GetEnumerator() => new(query, tableSchema);
    
    IEnumerator<OperationBlueprint> IEnumerable<OperationBlueprint>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}