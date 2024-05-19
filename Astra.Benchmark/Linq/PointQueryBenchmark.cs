using Astra.Client.Aggregator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark.Linq;

[SimpleJob(RuntimeMoniker.Net80)]
public class PointQueryBenchmark : BaseLinqBenchmark
{
    [Benchmark]
    public override void Astra()
    {
        var set = AstraStore.Aggregate(AstraTable<int, int, string>.Column1.EqualsLiteral(RecordCount - 1).DumpMemory());
        foreach (var value in set)
        {
            ProfessionalTimeWaster(value);
            break;
        }
    }

    [Benchmark]
    public override void AmortizedList()
    {
        var result = AmortizedListStore.FirstOrDefault(s => s.Data1 == RecordCount - 1);
        ProfessionalTimeWaster(result);
    }

    [Benchmark]
    public override void HashMap()
    {
        var result = HashMapStore[RecordCount - 1];
        ProfessionalTimeWaster(result);
    }

    [Benchmark]
    public override void RbTree()
    {
        var result = RbTreeStore[RecordCount - 1];
        ProfessionalTimeWaster(result);
    }
}