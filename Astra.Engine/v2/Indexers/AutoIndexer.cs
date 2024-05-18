using System.Reflection;
using Astra.Common.Data;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Planners.Physical;

namespace Astra.Engine.v2.Indexers;

public class AutoIndexer() : BaseIndexer(null!)
{
    private static readonly uint[] NoFeature = [ ];

    internal readonly HashSet<DataRow> Data = new();


    protected override IEnumerator<DataRow> GetEnumerator()
    {
        using var latch = Latch.Read();
        foreach (var row in Data)
        {
            yield return row;
        }
    }

    protected override bool Contains(DataRow row)
    {
        using var latch = Latch.Read();
        return Data.Contains(row);
    }
    protected override HashSet<DataRow> Fetch(ref readonly OperationBlueprint blueprint)
    {
        throw new NotSupportedException();
    }

    protected override HashSet<DataRow> Fetch(Stream predicateStream)
    {
        throw new NotSupportedException();
    }

    protected override HashSet<DataRow> Fetch(uint operation, Stream predicateStream)
    {
        throw new NotSupportedException();
    }

    protected override bool Add(DataRow row)
    {
        using var latch = Latch.Write();
        return Data.Add(row);
    }

    protected override bool Remove(DataRow row)
    {
        using var latch = Latch.Write();
        return Data.Remove(row);
    }

    protected override void Clear()
    {
        using var latch = Latch.Write();
        Data.Clear();
    }
    
    public bool SynchronizedInsert(DataRow row, ReadOnlySpan<Writer?> writers)
    {
        using var latch = Latch.Write();
        if (!Data.Add(row))
        {
            row.Dispose();
            return false;
        }

        foreach (var writer in writers)
        {
            writer?.Add(row);
        }

        return true;
    }
    
    public int SynchronizedRemove<TRows>(TRows enumerator, ReadOnlySpan<Writer?> writers) where TRows : IEnumerator<DataRow>
    {
        try
        {
            using var latch = Latch.Write();
            var i = 0;
            while (enumerator.MoveNext())
            {
                using var row = enumerator.Current;
                foreach (var writer in writers)
                {
                    writer?.Remove(row);
                }

                Data.Remove(row);
                i++;
            }
            
            return i;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    public int SynchronizedClear(ReadOnlySpan<Writer?> writers)
    {
        using var latch = Latch.Write();
        foreach (var writer in writers)
        {
            writer?.Clear();
        }
        var count = Data.Count;
        foreach (var row in Data)
        {
            row.Dispose();
        }
        Data.Clear();
        return count;
    }

    internal override MethodInfo GetFetchImplementation(uint operation)
    {
        throw new NotSupportedException();
    }

    protected override int Count
    {
        get
        {
            using var latch = Latch.Read();
            return Data.Count;
        }
    }

    public override FeaturesList SupportedReadOperations => NoFeature;
    public override FeaturesList SupportedWriteOperations => NoFeature;
    public override uint Priority => 0;
    public override DataType Type => DataType.None;
}