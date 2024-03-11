using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Engine.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalBulkInsertionBenchmark
{
    private DataRegistry _registry = null!;
    private SimpleSerializableStruct[] _data = null!;
    
    private int _bulkCounter = 1;
    
    [Params(10, 100, 1_000, 10_000)]
    public uint BulkInsertAmount;
    
    [IterationSetup]
    public void SetUp()
    {
        _data = new SimpleSerializableStruct[BulkInsertAmount];
        for (var i = 0; i < BulkInsertAmount; i++)
        {
            _data[i] = new()
            {
                Value1 = ++_bulkCounter,
                Value2 = "𝄞",
                Value3 = "🇵🇱",
            };
        }
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _data = null!;
    }

    [GlobalSetup]
    public void FirstSetup()
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
            }
        });
        DynamicSerializable.BuildSerializer<SimpleSerializableStruct>();
    }
    
    [GlobalCleanup]
    public void FinalCleanUp()
    {
        _registry.Dispose();
    }

    [Benchmark]
    public void ManualSerialization()
    {
        _registry.BulkInsertCompat(_data);
    }
    
    [Benchmark]
    public void AutoSerialization()
    {
        _registry.BulkInsert(_data);
    }
}