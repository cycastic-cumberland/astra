using System.Runtime.CompilerServices;
using Astra.Client.Simple.Aggregator;
using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Engine.Data;
using Astra.Engine.v2.Codegen;
using Astra.Engine.v2.Data;
using Astra.TypeErasure.Planners.Physical;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalAggregationBenchmark
{
    private DataRegistry _registry = null!;
    private ShinDataRegistry _newRegistry = null!;
    private PhysicalPlan _plan;
    private CompiledPhysicalPlan _compiledPlan;
    private const int Index = 1;

    [Params(100, 1_000, 10_000)]
    public uint AggregatedRows;

    private uint GibberishRows => AggregatedRows / 2;

    [GlobalSetup]
    public void GlobalSetUp()
    {
        DynamicSerializable.EnsureBuilt<SimpleSerializableStruct>();
        _plan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(Index).Build();
    }
    
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _plan.Dispose();
    }
    
    [IterationSetup]
    public void SetUp()
    {
        var specs = new RegistrySchemaSpecifications
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
        };
        _registry = new(specs);
        _newRegistry = new(specs);
        
        var plan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(Index).Build();
        _compiledPlan = _newRegistry.Compile(plan);
        
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
        _newRegistry.BulkInsertCompat(data);
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _registry.Dispose();
        _newRegistry.Dispose();
        _compiledPlan.Dispose();
        _registry = null!;
        _newRegistry = null!;
        _compiledPlan = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ProfessionalTimeWaster<T>(T _) {  }
    
    [Benchmark]
    public void ManualDeserialization()
    {
        var predicate = AstraTable<int, string, string, byte[], float>.Column1.EqualsLiteral(Index);
        var fetched = _registry.AggregateCompat<SimpleSerializableStruct>(
            predicate.DumpMemory());
        
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
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
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void ManualDeserializationPlanned()
    {
        var fetched = _newRegistry.AggregateCompat<SimpleSerializableStruct>(ref _plan);
        
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void AutoDeserializationPlanned()
    {
        var fetched = _newRegistry.Aggregate<SimpleSerializableStruct>(ref _plan);
        
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void ManualDeserializationPlannedCompiled()
    {
        var fetched = _newRegistry.AggregateCompat<SimpleSerializableStruct>(ref _compiledPlan);
        
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void AutoDeserializationPlannedCompiled()
    {
        var fetched = _newRegistry.Aggregate<SimpleSerializableStruct>(ref _compiledPlan);
        
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
}
