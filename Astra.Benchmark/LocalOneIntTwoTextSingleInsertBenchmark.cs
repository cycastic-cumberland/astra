using Astra.Engine;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalOneIntTwoTextSingleInsertBenchmark
{
    private DataIndexRegistry _registry = null!;
    
    private int _singularCounter = 1;
    
    [Params(10, 100, 1_000)]
    public uint RepeatCount;

    
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
    }
    
    [GlobalCleanup]
    public void FinalCleanUp()
    {
        _registry.Dispose();
    }
    
    [Benchmark]
    public void SingleRowInsertionBenchmark()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            using var inStream = BytesCluster.Rent(64).Promote();
            using var outStream = BytesCluster.Rent(32).Promote();
            inStream.WriteValue(1); // 1 Command
            inStream.WriteValue(Command.UnsortedInsert); // That command is insert
            
            inStream.WriteValue(1); // Insert this much rows
            
            inStream.WriteValue(_singularCounter++);
            inStream.WriteValue("test1");
            inStream.WriteValue("test2");
    
            inStream.Position = 0;
        
            _registry.ConsumeStream(inStream, outStream);
        }
    }
}