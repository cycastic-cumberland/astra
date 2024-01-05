using Astra.Engine;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Microsoft.IO;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class LocalOneIntTwoTextBulkInsertBenchmark
{
    private RecyclableMemoryStream _inStream = null!;
    private RecyclableMemoryStream _outStream = null!;
    private DataIndexRegistry _registry = null!;
    
    private int _bulkCounter = 1;
    
    [Params(10, 100, 1_000)]
    public uint BulkInsertRowsCount;
    
    [IterationSetup]
    public void SetUp()
    {
        _outStream = MemoryStreamPool.Allocate();
        _inStream = MemoryStreamPool.Allocate();
        _inStream.WriteValue(1); // 1 Command
        _inStream.WriteValue(Command.UnsortedInsert); // That command is insert
            
        _inStream.WriteValue(BulkInsertRowsCount); // Insert this much rows
            
    
        for (var j = 0U; j < BulkInsertRowsCount; j++)
        {
            _inStream.WriteValue(_bulkCounter++);
            _inStream.WriteValue("test1");
            _inStream.WriteValue("test2");
        }
    
        _inStream.Position = 0;
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _inStream.Dispose();
        _outStream.Dispose();
        _inStream = null!;
        _outStream = null!;
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
    public void PureBulkInsertionBenchmark()
    {
        _registry.ConsumeStream(_inStream, _outStream);
    }
}