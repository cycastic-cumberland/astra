using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Engine.Data;
using Astra.Engine.v2.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalBulkInsertionBenchmark
{
    private DataRegistry _registry = null!;
    private ShinDataRegistry _newRegistry = null!;
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
        var specs = new RegistrySchemaSpecifications()
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
        };
        _registry = new(specs);
        _newRegistry = new(specs);
        
        DynamicSerializable.EnsureBuilt<SimpleSerializableStruct>();
    }
    
    [GlobalCleanup]
    public void FinalCleanUp()
    {
        _registry.Dispose();
        _newRegistry.Dispose();
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
    
    [Benchmark]
    public void ManualSerializationNew()
    {
        _newRegistry.BulkInsertCompat(_data);
    }
    
    [Benchmark]
    public void AutoSerializationNew()
    {
        _newRegistry.BulkInsert(_data);
    }
}