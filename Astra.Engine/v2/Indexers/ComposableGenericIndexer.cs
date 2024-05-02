using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Indexers;
using Astra.Engine.v2.Data;

namespace Astra.Engine.v2.Indexers;

public class GenericIndexer(ColumnSchema schema) : BaseIndexer(schema)
{
    private static readonly uint[] GenericFeatures = [ Operation.Equal ];
    // private static readonly uint[] NumericFeatures = [ 
    //     Operation.Equal, 
    //     Operation.ClosedBetween,
    //     Operation.GreaterThan,
    //     Operation.GreaterOrEqualsTo,
    //     Operation.LessThan,
    //     Operation.LesserOrEqualsTo
    // ];
    
    private readonly Dictionary<DataCell, HashSet<DataRow>> _data = new();


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

    protected override IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream)
    {
        return operation switch
        {
            Operation.Equal => CollectExact(predicateStream),
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

    public override FeaturesList SupportedReadOperations => GenericFeatures;
    public override FeaturesList SupportedWriteOperations => GenericFeatures;
    public override uint Priority => 0;
    public override DataType Type => Schema.Type;
}