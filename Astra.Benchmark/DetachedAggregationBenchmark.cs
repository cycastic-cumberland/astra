using System.Runtime.CompilerServices;
using Astra.Client;
using Astra.TypeErasure.Planners.Physical;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class DetachedAggregationBenchmark
{
    public const int Port = 8499;
    private const int Index = 1;
    private AstraClient _client = null!;
    
    [Params(1_000, 10_000)]
    public uint AggregatedRows;

    private PhysicalPlan _plan;
    private uint GibberishRows => AggregatedRows / 2;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Task.Run(async () =>
        {
            _client = new();
        
            await _client.ConnectAsync(new()
            {
                Address = "127.0.0.1",
                Port = Port,
            });
            var data = new SimpleSerializableStruct[AggregatedRows + GibberishRows];
            for (var i = 0; i < AggregatedRows; i++)
            {
                data[i] = new()
                {
                    Value1 = Index, // 4 bytes
                    Value2 = "test", // 4 + 4 bytes
                    Value3 = i.ToString() // 4 + (<= 4) bytes
                };
            }

            for (var i = AggregatedRows; i < AggregatedRows + GibberishRows; i++)
            {
                data[i] = new()
                {
                    Value1 = Index + unchecked((int)i),
                    Value2 = "test",
                    Value3 = i.ToString()
                };
            }

            await _client.BulkInsertSerializableCompatAsync(data);
        }).Wait();
        _plan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(Index).Build();
    }
    
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _client.ClearAsync().Wait();
        _client.Dispose();
        _client = null!;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ProfessionalTimeWaster<T>(T _) {  }

    private async Task SimpleAggregationAndAutoDeserializationBenchmarkNewAsync()
    {
        using var fetched = await _client.AggregateAsync<SimpleSerializableStruct>(_plan);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void AutoDeserializationNew()
    {
        SimpleAggregationAndAutoDeserializationBenchmarkNewAsync().Wait();
    }
}