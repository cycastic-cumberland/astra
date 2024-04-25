using Astra.Client.Simple.Aggregator;
using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Engine.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalAggregationBenchmark
{
    private DataRegistry _registry = null!;
    private const int Index = 1;

    [Params(100, 1_000, 10_000, 100_000)]
    public uint AggregatedRows;

    private uint GibberishRows => AggregatedRows / 2;

    [GlobalSetup]
    public void GlobalSetUp()
    {
        DynamicSerializable.EnsureBuilt<SimpleSerializableStruct>();
    }
    
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
                    Indexer = IndexerType.BTree,
                },
                new()
                {
                    Name = "col2",
                    DataType = DataType.StringMask,
                    Indexer = IndexerType.None,
                },
                new()
                {
                    Name = "col3",
                    DataType = DataType.StringMask,
                    Indexer = IndexerType.Generic,
                }
            },
            BinaryTreeDegree = (int)(AggregatedRows / 10)
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

        _registry.BulkInsertCompat(data);
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _registry.Dispose();
        _registry = null!;
    }

    private ulong _a;
    
    [Benchmark]
    public void ManualDeserialization()
    {
        var predicate = AstraTable<int, string, string, byte[], float>.Column1.EqualsLiteral(Index);
        var fetched = _registry.AggregateCompat<SimpleSerializableStruct>(
            predicate.DumpMemory());
        
        foreach (var f in fetched)
        {
            _a += unchecked((ulong)f.Value1);
        }
    }
    
    [Benchmark]
    public void AutoDeserialization()
    {
        var predicate = AstraTable<int, string, string, byte[], float>.Column1.EqualsLiteral(Index);
        var fetched = _registry.Aggregate<SimpleSerializableStruct>(
            predicate.DumpMemory());
        
        foreach (var f in fetched)
        {
            _a += unchecked((ulong)f.Value1);
        }
    }
}
