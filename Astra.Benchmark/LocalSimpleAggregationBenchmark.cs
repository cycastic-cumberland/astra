using Astra.Client.Aggregator;
using Astra.Engine;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalSimpleAggregationBenchmark
{
    private readonly AstraTable<int, string, string> _table = new();
    private DataIndexRegistry _registry = null!;
    private const int Index = 1;

    [Params(100, 1_000, 10_000)]
    public uint AggregatedRows;

    private uint GibberishRows => AggregatedRows / 2;

    private static int Stuff(uint a) => unchecked((int)a);
    private static uint Stuff(int a) => unchecked((uint)a);
    
    [IterationSetup]
    public void SetUp()
    {
        _registry = new(new() 
        {
            Columns = new ColumnSchemaSpecifications[] 
            {
                new()
                {
                    Name = "col1",
                    DataType = DataType.DWordMask,
                    Indexed = true,
                },
                new()
                {
                    Name = "col2",
                    DataType = DataType.StringMask,
                    Indexed = false,
                },
                new()
                {
                    Name = "col3",
                    DataType = DataType.StringMask,
                    Indexed = true,
                }
            }
        });
        var data = new SimpleSerializableStruct[AggregatedRows + GibberishRows];
        for (var i = 0; i < AggregatedRows; i++)
        {
            data[i] = new()
            {
                Value1 = Index,
                Value2 = "test",
                Value3 = i.ToString()
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

        _registry.BulkInsert(data);
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _registry.Dispose();
        _registry = null!;
    }

    [Benchmark]
    public void SimpleAggregationBenchmark()
    {
        var predicate = _table.Column1.EqualsLiteral(Index);
        _ = _registry.Aggregate<SimpleSerializableStruct>(
            predicate.DumpMemory());
        
    }

    private ulong _a;
    
    [Benchmark]
    public void SimpleAggregationAndDeserializationBenchmark()
    {
        var predicate = _table.Column1.EqualsLiteral(Index);
        var fetched = _registry.Aggregate<SimpleSerializableStruct>(
            predicate.DumpMemory());
        
        foreach (var f in fetched)
        {
            _a += unchecked((ulong)f.Value1);
        }
    }
}
