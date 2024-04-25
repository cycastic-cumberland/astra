using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark.Linq;

[SimpleJob(RuntimeMoniker.Net80)]
public class ForwardIterationBenchmark : BaseLinqBenchmark
{
    [Benchmark]
    public override void Astra()
    {
        foreach (var value in AstraStore)
        {
            ProfessionalTimeWaster(value);
        }
    }

    [Benchmark]
    public override void AmortizedList()
    {
        foreach (var value in AmortizedListStore)
        {
            ProfessionalTimeWaster(value);
        }
    }

    [Benchmark]
    public override void HashMap()
    {
        foreach (var value in HashMapStore)
        {
            ProfessionalTimeWaster(value);
        }
    }

    [Benchmark]
    public override void RbTree()
    {
        foreach (var value in RbTreeStore)
        {
            ProfessionalTimeWaster(value);
        }
    }
}