using System.Runtime.CompilerServices;
using Astra.Common.Data;
using Astra.Engine.Data;
using Astra.Engine.Data.Attributes;
using BenchmarkDotNet.Attributes;

namespace Astra.Benchmark.Linq;

public abstract class BaseLinqBenchmark
{
    public struct SampleStruct
    {
        [Indexed(Indexer = IndexerType.BTree)]
        public int Data1 { get; set; }
        [Indexed(Indexer = IndexerType.Generic)]
        public int Data2 { get; set; }
        public string Data3 { get; set; }
    }
    
    protected DataRegistry<SampleStruct> AstraStore = null!;
    protected List<SampleStruct> AmortizedListStore = null!;
    protected Dictionary<int, SampleStruct> HashMapStore = null!;
    protected SortedDictionary<int, SampleStruct> RbTreeStore = null!;
    protected string StringUsed = string.Empty;

    [Params(1_000, 10_000)]
    public int RecordCount;

    [Params(0, 1_000)]
    public int StringLength;
    
    [IterationSetup]
    public void IterationSetUp()
    {
        AstraStore = new(new()
        {
            DefaultIndexerType = IndexerType.None
        });
        AmortizedListStore = new(RecordCount);
        HashMapStore = new();
        RbTreeStore = new();
        StringUsed = new string((char)42, StringLength);
        
        for (var i = 0; i < RecordCount; i++)
        {
            AmortizedListStore.Add(new()
            {
                Data1 = i,
                Data2 = i % 3,
                Data3 = StringUsed
            });
        }

        foreach (var value in AmortizedListStore)
        {
            AstraStore.Add(value);
            HashMapStore[value.Data1] = value;
            RbTreeStore[value.Data1] = value;
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        AstraStore.Dispose();
    }


    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    protected static void ProfessionalTimeWaster<T>(T _) {  }
    
    public abstract void Astra();
    
    public abstract void AmortizedList();
    
    public abstract void HashMap();
    
    public abstract void RbTree();
}