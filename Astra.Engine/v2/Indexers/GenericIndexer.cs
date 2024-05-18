using System.Diagnostics;
using System.Reflection;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Indexers;

public class GenericIndexer(ColumnSchema schema) : BaseIndexer(schema)
{
    private static readonly uint[] GenericFeatures = [ Operation.Equal ];
    
    private readonly Dictionary<DataCell, HashSet<DataRow>> _data = new();
    private readonly MethodInfo _collectExactImpl = typeof(GenericIndexer).GetMethod(nameof(CollectExact),
                                                        [typeof(DataCell).MakeByRefType()]) ??
                                                    throw new UnreachableException();

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
        using var latch = Latch.Read();
        _data.TryGetValue(value, out var rows);
        return rows;
    }

    protected override IEnumerator<DataRow> GetEnumerator()
    {
        using var latch = Latch.Read();
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
        using var latch = Latch.Read();
        return _data.TryGetValue(row.Span[Schema.Index], out var set) && set.Contains(row);
    }

    protected override HashSet<DataRow>? Fetch(ref readonly OperationBlueprint blueprint)
    {
        using var latch = Latch.Read();
        return blueprint.PredicateOperationType switch
        {
            Operation.Equal => CollectExact(in blueprint)?.ToHashSet(),
            _ => throw new OperationNotSupported($"Operation not supported: {blueprint.PredicateOperationType}")
        };
    }

    protected override HashSet<DataRow>? Fetch(Stream predicateStream)
    {
        var op = predicateStream.ReadUInt();
        return Fetch(op, predicateStream);
    }

    protected override HashSet<DataRow>? Fetch(uint operation, Stream predicateStream)
    {
        using var latch = Latch.Read();
        return operation switch
        {
            Operation.Equal => CollectExact(predicateStream)?.ToHashSet(),
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }

    protected override bool Add(DataRow row)
    {
        using var latch = Latch.Write();
        var key = row.Span[Schema.Index];
        if (!_data.TryGetValue(key, out var set))
        {
            set = new();
            _data[key] = set;
        }

        return set.Add(row);
    }

    protected override bool Remove(DataRow row)
    {
        using var latch = Latch.Write();
        ref readonly var cond = ref row.Span[Schema.Index];
        if (!_data.TryGetValue(cond, out var set)) return false;
        return set.Remove(row);
    }

    protected override void Clear()
    {
        using var latch = Latch.Write();
        _data.Clear();
    }

    internal override MethodInfo GetFetchImplementation(uint operation)
    {
        return operation switch
        {
            Operation.Equal => _collectExactImpl,
            _ => throw new OperationNotSupported($"Operation not supported: {operation}")
        };
    }

    protected override int Count
    {
        get
        {
            using var latch = Latch.Read();
            return _data.Count;
        }
    }

    public override FeaturesList SupportedReadOperations => GenericFeatures;
    public override FeaturesList SupportedWriteOperations => GenericFeatures;
    public override uint Priority => 0;
    public override DataType Type => Schema.Type;
}