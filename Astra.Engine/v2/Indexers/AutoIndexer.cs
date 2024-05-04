using Astra.Common.Data;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Planners;

namespace Astra.Engine.v2.Indexers;

public class AutoIndexer() : BaseIndexer(null!)
{
    private static readonly uint[] NoFeature = [ ];

    private readonly HashSet<DataRow> _data = new();

    internal HashSet<DataRow> Probe() => _data;

    protected override IEnumerator<DataRow> GetEnumerator() => _data.GetEnumerator();

    protected override bool Contains(DataRow row) => _data.Contains(row);
    protected override HashSet<DataRow>? Fetch(ref readonly OperationBlueprint blueprint)
    {
        throw new NotSupportedException();
    }

    protected override HashSet<DataRow>? Fetch(Stream predicateStream)
    {
        throw new NotSupportedException();
    }

    protected override IEnumerable<DataRow>? Fetch(uint operation, Stream predicateStream)
    {
        throw new NotSupportedException();
    }

    protected override bool Add(DataRow row) => _data.Add(row);

    protected override IEnumerable<DataRow>? Remove(Stream predicateStream)
    {
        throw new NotSupportedException();
    }

    protected override bool Remove(DataRow row)
    {
        return _data.Remove(row);
    }

    protected override void Clear()
    {
        _data.Clear();
    }

    protected override int Count => _data.Count;

    public override FeaturesList SupportedReadOperations => NoFeature;
    public override FeaturesList SupportedWriteOperations => NoFeature;
    public override uint Priority => 0;
    public override DataType Type => DataType.None;
}