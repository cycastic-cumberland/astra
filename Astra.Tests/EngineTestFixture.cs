using System.Diagnostics;
using System.Net.Sockets;
using Astra.Engine;
using Astra.Server;
using Microsoft.Extensions.Logging;

namespace Astra.Tests;

[TestFixture]
public class EngineTestFixture
{
    private struct TinySerializableStruct : IAstraSerializable
    {
        public int Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; }
        
        public void SerializeStream<TStream>(TStream writer) where TStream : Stream
        {
            writer.WriteValue(Value1);
            writer.WriteValue(Value2);
            writer.WriteValue(Value3);
        }

        public void DeserializeStream<TStream>(TStream reader) where TStream : Stream
        {
            Value1 = reader.ReadInt();
            Value2 = reader.ReadString();
            Value3 = reader.ReadString();
        }
    }
    private ILoggerFactory _loggerFactory = null!;
    private DataIndexRegistry _registry = null!;

    private readonly ColumnSchemaSpecifications[] _columns = {
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
    };
    [OneTimeSetUp]
    public void Setup()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _registry = new(new()
        {
            Columns = _columns
        }, _loggerFactory);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _registry.Dispose();
        _loggerFactory.Dispose();
    }

    // [Test]
    // [Parallelizable]
    // public void InsertionBenchmark()
    // {
    //     const uint iterationCount = 5000;
    //     const uint bulkInsertRowsCount = 2000;
    //     var num = 0;
    //     var totalTime = 0.0;
    //     var stopwatch = new Stopwatch();
    //     var primaryStopwatch = new Stopwatch();
    //     using var registry = new DataIndexRegistry(new()
    //     {
    //         Columns = new ColumnSchemaSpecifications[]
    //         {
    //             new()
    //             {
    //                 Name = "col1",
    //                 DataType = DataType.DWordMask,
    //                 Indexed = true,
    //             },
    //             new()
    //             {
    //                 Name = "col2",
    //                 DataType = DataType.StringMask,
    //                 Indexed = false,
    //             },
    //             new()
    //             {
    //                 Name = "col3",
    //                 DataType = DataType.StringMask,
    //                 Indexed = true,
    //             }
    //         }
    //     });
    //     primaryStopwatch.Start();
    //     for (var i = 0U; i < iterationCount; i++)
    //     {
    //         using var inStream = MemoryStreamPool.Allocate();
    //         using var outStream = MemoryStreamPool.Allocate();
    //         inStream.WriteValue(Command.CreateWriteHeader(1U)); // 1 Command
    //         inStream.WriteValue(Command.UnsortedInsert); // That command is insert
    //         
    //         inStream.WriteValue(bulkInsertRowsCount); // Insert this much rows
    //         
    //
    //         for (var j = 0U; j < bulkInsertRowsCount; j++)
    //         {
    //             inStream.WriteValue(num++);
    //             inStream.WriteValue("test1");
    //             inStream.WriteValue("test2");
    //         }
    //
    //         inStream.Position = 0;
    //         stopwatch.Start();
    //         registry.ConsumeStream(inStream, outStream);
    //         stopwatch.Stop();
    //
    //         totalTime += stopwatch.Elapsed.TotalMicroseconds;
    //         
    //         stopwatch.Reset();
    //     }
    //     primaryStopwatch.Stop();
    //     var pureBulkInsertionTime = totalTime / iterationCount;
    //     var totalElapsedTime = primaryStopwatch.Elapsed.TotalMicroseconds;
    //     var prepTime = totalElapsedTime - totalTime;
    //     var prepPercentile = prepTime / totalElapsedTime;
    //     
    //     Console.WriteLine($"Average time to insert {bulkInsertRowsCount} rows: {pureBulkInsertionTime} us");
    //     Console.WriteLine($"Average time to insert 1 row: {pureBulkInsertionTime / bulkInsertRowsCount} us");
    //     Console.WriteLine($"Total time: {totalElapsedTime} us");
    //     Console.WriteLine($"Preparation time: {prepTime} us ({prepPercentile * 100.0}%)");
    //     var rowsCount = registry.RowsCount;
    //     Console.WriteLine($"Rows count: {rowsCount}");
    //     Assert.That(rowsCount, Is.EqualTo(iterationCount * bulkInsertRowsCount));
    // }

    [Test]
    public void LocalRegistryTest()
    {
        var inserted = _registry.BulkInsert(new TinySerializableStruct[]
        {
            new()
            {
                Value1 = 1,
                Value2 = "test1",
                Value3 = "test2",
            },
            new()
            {
                Value1 = 2,
                Value2 = "test3",
                Value3 = "test4",
            },
            new()
            {
                Value1 = 2,
                Value2 = "test6",
                Value3 = "test4",
            },
        });
        Assert.That(inserted, Is.EqualTo(2));
        using var predicateStream = MemoryStreamPool.Allocate();
        predicateStream.WriteValue(PredicateType.UnaryMask); // type
        predicateStream.WriteValue(0); // indexer offset
        predicateStream.WriteValue(Operation.Equal); // operation type
        predicateStream.WriteValue(DataType.DWordMask); // data type
        predicateStream.WriteValue(2); // Value to compare against
        predicateStream.Position = 0;
        var deserialized = _registry.Aggregate<TinySerializableStruct>(predicateStream);
        var pass = true;
        foreach (var row in deserialized)
        {
            if (!pass) Assert.Fail();
            Assert.Multiple(() =>
            {
                Assert.That(row.Value1, Is.EqualTo(2));
                Assert.That(row.Value2, Is.EqualTo("test3"));
                Assert.That(row.Value3, Is.EqualTo("test4"));
            });
            pass = false;
        }

        predicateStream.Position = 0;
        var deleted = _registry.Delete(predicateStream);
        Assert.That(deleted, Is.EqualTo(1));
        Assert.That(_registry.RowsCount, Is.EqualTo(1));
    }
}