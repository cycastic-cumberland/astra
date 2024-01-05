using System.Diagnostics;
using System.Net.Sockets;
using Astra.Engine;
using Astra.Server;
using Microsoft.Extensions.Logging;

namespace Astra.Tests;

[TestFixture]
public class EngineTestFixture
{
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
    //         inStream.WriteValue(1); // 1 Command
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
    public void LocalInsertionTest()
    {
        var inStream = MemoryStreamPool.Allocate();
        using var outStream = MemoryStreamPool.Allocate();
        inStream.WriteValue(1U); // 1 Command
        inStream.WriteValue(Command.UnsortedInsert); // That command is insert
        
        inStream.WriteValue(3); // Insert three rows
        
        inStream.WriteValue(11);
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
        inStream.WriteValue(12);
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
        inStream.WriteValue(11); // Duplicated!
        inStream.WriteValue("test3");
        inStream.WriteValue("test4");
    
        inStream.Position = 0;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        _registry.ConsumeStream(inStream, outStream);
        stopwatch.Stop();
        Console.WriteLine("Inserted 3 rows in {0} us", stopwatch.Elapsed.TotalMicroseconds);
    
        outStream.Position = 0;
        var faulted = unchecked((byte)outStream.ReadByte());
        Assert.That(faulted, Is.Zero);
        var inserted = outStream.ReadInt();
        Assert.That(inserted, Is.EqualTo(2));
    }
    
    [Test]
    [Parallelizable]
    public async Task SimpleNetworkInsertionTest()
    {
        var server = new TcpServer(new()
        {
            LogLevel = "Debug",
            Schema = new()
            {
                Columns = _columns
            }
        });
        try
        {
#pragma warning disable CS4014
            Task.Run(() => server.RunAsync());
#pragma warning restore CS4014
            await Task.Delay(100);
            {
                using var client = new TcpClient("127.0.0.1", TcpServer.DefaultPort);
                var networkStream = client.GetStream();
                while (client.Available < sizeof(int)) await Task.Delay(100);
                {
                    using var checkEndianness = BytesCluster.Rent(sizeof(int));
                    _ = await networkStream.ReadAsync(checkEndianness.WriterMemory);
                    var isLittleEndian = checkEndianness.Reader[0] == 1;
                    Assert.That(isLittleEndian, Is.EqualTo(BitConverter.IsLittleEndian));
                }
                await using var inStream = MemoryStreamPool.Allocate();
                inStream.WriteValue(1U); // 1 Command
                inStream.WriteValue(Command.UnsortedInsert); // That command is insert
            
                inStream.WriteValue(3); // Insert three rows
            
                inStream.WriteValue(11);
                inStream.WriteValue("test3");
                inStream.WriteValue("test4");
                inStream.WriteValue(12);
                inStream.WriteValue("test3");
                inStream.WriteValue("test4");
                inStream.WriteValue(11); // Duplicated!
                inStream.WriteValue("test3");
                inStream.WriteValue("test4");
                await networkStream.WriteValueAsync(inStream.Length);
                await networkStream.WriteAsync(new ReadOnlyMemory<byte>(inStream.GetBuffer(), 0, (int)inStream.Length));
                while (client.Available < sizeof(long)) await Task.Delay(100);
                var outStreamSize = await networkStream.ReadLongAsync();
                var cluster = BytesCluster.Rent((int)outStreamSize);
                while (client.Available < outStreamSize) await Task.Delay(100);
                var read = await networkStream.ReadAsync(cluster.WriterMemory);
                Assert.That(read, Is.EqualTo(outStreamSize));
                await using var outStream = cluster.Promote();
                var faulted = (byte)outStream.ReadByte();
                Assert.That(faulted, Is.Zero);
                var inserted = outStream.ReadInt();
                Assert.That(inserted, Is.EqualTo(2));
                await Task.Delay(100);
            }
        }
        finally
        {
            server.Kill();
        }
    }
}