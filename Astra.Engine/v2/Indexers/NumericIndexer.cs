using Astra.Collections.RangeDictionaries;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Indexers;
using Astra.Engine.v2.Data;

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

    protected override int Count => _data.Count;

    public override FeaturesList SupportedReadOperations => NumericFeatures;
    public override FeaturesList SupportedWriteOperations => NumericFeatures;
    public override uint Priority => 0;
    public override DataType Type => Schema.Type;
}