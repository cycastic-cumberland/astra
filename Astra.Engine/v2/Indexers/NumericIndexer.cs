using System.Diagnostics;
using System.Reflection;
using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Indexers;

public class NumericIndexer : BaseIndexer
{
    private static readonly uint[] NumericFeatures = [ 
        Operation.Equal, 
        Operation.ClosedBetween,
        Operation.GreaterThan,
        Operation.GreaterOrEqualsTo,
        Operation.LesserThan,
        Operation.LesserOrEqualsTo
    ];
    
    private readonly BTreeMap<DataCell, HashSet<DataRow>> _data;
    private readonly MethodInfo _collectExactImpl = typeof(NumericIndexer).GetMethod(nameof(CollectExact),
                                                        [typeof(DataCell).MakeByRefType()]) ??
                                                    throw new UnreachableException();
    private readonly MethodInfo _closedBetweenImpl = typeof(NumericIndexer).GetMethod(nameof(ClosedBetween),
                                                     [
                                                         typeof(DataCell).MakeByRefType(),
                                                         typeof(DataCell).MakeByRefType()
                                                     ]) ??
                                                     throw new UnreachableException();
    private readonly MethodInfo _greaterImpl = typeof(NumericIndexer).GetMethod(nameof(GreaterThan),
                                                   [typeof(DataCell).MakeByRefType()]) ??
                                               throw new UnreachableException();
    private readonly MethodInfo _greaterOrEqualImpl = typeof(NumericIndexer).GetMethod(nameof(GreaterThanOrEqualTo),
                                                          [typeof(DataCell).MakeByRefType()]) ??
                                                      throw new UnreachableException();
    private readonly MethodInfo _lessImpl = typeof(NumericIndexer).GetMethod(nameof(LessThan),
                                                [typeof(DataCell).MakeByRefType()]) ??
                                            throw new UnreachableException();
    private readonly MethodInfo _lessOrEqualImpl = typeof(NumericIndexer).GetMethod(nameof(LessThanOrEqualTo),
                                                       [typeof(DataCell).MakeByRefType()]) ??
                                                   throw new UnreachableException();

    public NumericIndexer(ColumnSchema schema) : base(schema)
    {
        _data = new(schema.BTreeDegree);
    }

    private HashSet<DataRow>? CollectExact(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var cond = DataCell.FromStream(Schema.Type.Value, predicateStream);
        _data.TryGetValue(cond, out var rows);
        return rows;
    }
    
    private HashSet<DataRow>? CollectExact(ref readonly OperationBlueprint blueprint)
    {
        _data.TryGetValue(blueprint.Cell1, out var rows);
        return rows;
    }
    
    public HashSet<DataRow>? CollectExact(ref readonly DataCell value)
    {
        _data.TryGetValue(value, out var rows);
        return rows;
    }

    protected override IEnumerator<DataRow> GetEnumerator()
    {
        foreach (var (_, rows) in _data)
        {
            foreach (var row in rows)
            {
                yield return row;
            }
        }
    }

    protected override bool Contains(DataRow row)
    {
        return _data.TryGetValue(row.Span[Schema.Index], out var set) && set.Contains(row);
    }

    protected override IEnumerable<DataRow>? Fetch(ref readonly OperationBlueprint blueprint)
    {
        return blueprint.PredicateOperationType switch
        {
            Operation.Equal => CollectExact(in blueprint),
            Operation.ClosedBetween => ClosedBetween(in blueprint),
            Operation.GreaterThan => GreaterThan(in blueprint),
            Operation.GreaterOrEqualsTo => GreaterThanOrEqualTo(in blueprint),
            Operation.LesserThan => LessThan(in blueprint),
            Operation.LesserOrEqualsTo => LessThanOrEqualTo(in blueprint),
            _ => throw new OperationNotSupported($"Operation not supported: {blueprint.PredicateOperationType}")
        };
    }

    protected override IEnumerable<DataRow>? Fetch(Stream predicateStream)
    {
        var op = predicateStream.ReadUInt();
        return Fetch(op, predicateStream);
    }
    
    public IEnumerable<DataRow> ClosedBetween(DataCell left, DataCell right)
    {
        foreach (var (_, set) in _data.Collect(left, right, CollectionMode.ClosedInterval))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<DataRow> ClosedBetween(ref readonly OperationBlueprint blueprint)
    {
        return ClosedBetween(blueprint.Cell1, blueprint.Cell2);
    }
    
    public IEnumerable<DataRow> ClosedBetween(ref readonly DataCell left, ref readonly DataCell right)
    {
        return ClosedBetween(left, right);
    }
     
    private IEnumerable<DataRow> ClosedBetween(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var left = DataCell.FromStream(Schema.Type.Value, predicateStream);
        var right = DataCell.FromStream(Schema.Type.Value, predicateStream);
        return ClosedBetween(left, right);
    }

    public IEnumerable<DataRow> GreaterThan(DataCell left)
    {
        foreach (var (_, set) in _data.CollectFrom(left, false))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<DataRow> GreaterThan(ref readonly OperationBlueprint blueprint)
    {
        return GreaterThan(blueprint.Cell1);
    }
    
    public IEnumerable<DataRow> GreaterThan(ref readonly DataCell value)
    {
        return GreaterThan(value);
    }
    
    private IEnumerable<DataRow> GreaterThan(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var left = DataCell.FromStream(Schema.Type.Value, predicateStream);
        return GreaterThan(left);
    }

    public IEnumerable<DataRow> GreaterThanOrEqualTo(DataCell left)
    {
        foreach (var (_, set) in _data.CollectFrom(left, true))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<DataRow> GreaterThanOrEqualTo(ref readonly OperationBlueprint blueprint)
    {
        return GreaterThanOrEqualTo(blueprint.Cell1);
    }
    
    public IEnumerable<DataRow> GreaterThanOrEqualTo(ref readonly DataCell value)
    {
        return GreaterThanOrEqualTo(value);
    }
    
    private IEnumerable<DataRow> GreaterThanOrEqualTo(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var left = DataCell.FromStream(Schema.Type.Value, predicateStream);
        return GreaterThanOrEqualTo(left);
    }
    
    public IEnumerable<DataRow> LessThan(DataCell right)
    {
        foreach (var (_, set) in _data.CollectTo(right, false))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<DataRow> LessThan(ref readonly OperationBlueprint blueprint)
    {
        return LessThan(blueprint.Cell1);
    }
    
    public IEnumerable<DataRow> LessThan(ref readonly DataCell value)
    {
        return LessThan(value);
    }
    
    private IEnumerable<DataRow> LessThan(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var right = DataCell.FromStream(Schema.Type.Value, predicateStream);
        return LessThan(right);
    }
    
    public IEnumerable<DataRow> LessThanOrEqualTo(DataCell right)
    {
        foreach (var (_, set) in _data.CollectTo(right, true))
        {
            foreach (var row in set)
            {
                yield return row;
            }
        }
    }
    
    private IEnumerable<DataRow> LessThanOrEqualTo(ref readonly OperationBlueprint blueprint)
    {
        return LessThanOrEqualTo(blueprint.Cell1);
    }
    
    public IEnumerable<DataRow> LessThanOrEqualTo(ref readonly DataCell value)
    {
        return LessThanOrEqualTo(value);
    }
    
    private IEnumerable<DataRow> LessThanOrEqualTo(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var right = DataCell.FromStream(Schema.Type.Value, predicateStream);
        return LessThanOrEqualTo(right);
    }

    protected override IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream)
    {
        return operation switch
        {
            Operation.Equal => CollectExact(predicateStream),
            Operation.ClosedBetween => ClosedBetween(predicateStream),
            Operation.GreaterThan => GreaterThan(predicateStream),
            Operation.GreaterOrEqualsTo => GreaterThanOrEqualTo(predicateStream),
            Operation.LesserThan => LessThan(predicateStream),
            Operation.LesserOrEqualsTo => LessThanOrEqualTo(predicateStream),
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }

    protected override bool Add(DataRow row)
    {
        var key = row.Span[Schema.Index];
        if (!_data.TryGetValue(key, out var list))
        {
            list = new();
            _data[key] = list;
        }

        return list.Add(row);
    }

    protected override HashSet<DataRow>? Remove(Stream predicateStream)
    {
        predicateStream.CheckDataType(Schema.Type);
        var cond = DataCell.FromStream(Schema.Type.Value, predicateStream);
        if (_data.TryGetValue(cond, out var set)) return set;
        _data.Remove(cond);
        return set;
    }

    protected override bool Remove(DataRow row)
    {
        ref readonly var cond = ref row.Span[Schema.Index];
        if (!_data.TryGetValue(cond, out var set)) return false;
        return set.Remove(row);
    }

    protected override void Clear()
    {
        _data.Clear();
    }

    internal override MethodInfo GetFetchImplementation(uint operation)
    {
        return operation switch
        {
            Operation.Equal => _collectExactImpl,
            Operation.ClosedBetween => _closedBetweenImpl,
            Operation.GreaterThan => _greaterImpl,
            Operation.GreaterOrEqualsTo => _greaterOrEqualImpl,
            Operation.LesserThan => _lessImpl,
            Operation.LesserOrEqualsTo => _lessOrEqualImpl,
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }

    protected override int Count => _data.Count;

    public override FeaturesList SupportedReadOperations => NumericFeatures;
    public override FeaturesList SupportedWriteOperations => NumericFeatures;
    public override uint Priority => 0;
    public override DataType Type => Schema.Type;
}