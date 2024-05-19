using System.Runtime.CompilerServices;
using Astra.Client;
using Astra.Client.Aggregator;
using Astra.Common.Data;
using Astra.Server;
using Astra.Server.Authentication;
using Astra.TypeErasure.Planners.Physical;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class CompressedAggregationBenchmark
{
    public enum CompressionAlgorithms
    {
        GZip,
        Deflate,
        Brotli,
        ZLib
    }
    public enum CompressionStrategies
    {
        Optimal,
        Fastest,
        SmallestSize,
    }
    [Params(100, 1_000, 10_000)]
    public uint AggregatedRows;
    
    [Params(CompressionAlgorithms.GZip, CompressionAlgorithms.Deflate, CompressionAlgorithms.Brotli, CompressionAlgorithms.ZLib)]
    public CompressionAlgorithms Algorithm;
    
    [Params(CompressionStrategies.Optimal, CompressionStrategies.Fastest, CompressionStrategies.SmallestSize)]
    public CompressionStrategies Strategy;
    private uint GibberishRows => AggregatedRows / 2;
    
    
    private static readonly RegistrySchemaSpecifications Schema = new()
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

    private Dictionary<CompressionOptions, (TcpServer, Task, AstraClient)> _lookupTable = null!;
    private PhysicalPlan _plan;
    private const int Index = 1;
    
    private CompressionOptions SelectCompressionOption()
    {
        return (CompressionOptions)(Algorithm switch
        {
            CompressionAlgorithms.GZip => Strategy switch
            {
                CompressionStrategies.Optimal => ConnectionFlags.CompressionOptions.GZip |
                                                 ConnectionFlags.CompressionOptions.Optimal,
                CompressionStrategies.Fastest => ConnectionFlags.CompressionOptions.GZip |
                                                 ConnectionFlags.CompressionOptions.Fastest,
                CompressionStrategies.SmallestSize => ConnectionFlags.CompressionOptions.GZip |
                                                      ConnectionFlags.CompressionOptions.SmallestSize,
                _ => throw new ArgumentOutOfRangeException()
            },
            CompressionAlgorithms.Deflate => Strategy switch
            {
                CompressionStrategies.Optimal => ConnectionFlags.CompressionOptions.Deflate |
                                                 ConnectionFlags.CompressionOptions.Optimal,
                CompressionStrategies.Fastest => ConnectionFlags.CompressionOptions.Deflate |
                                                 ConnectionFlags.CompressionOptions.Fastest,
                CompressionStrategies.SmallestSize => ConnectionFlags.CompressionOptions.Deflate |
                                                      ConnectionFlags.CompressionOptions.SmallestSize,
                _ => throw new ArgumentOutOfRangeException()
            },
            CompressionAlgorithms.Brotli => Strategy switch
            {
                CompressionStrategies.Optimal => ConnectionFlags.CompressionOptions.Brotli |
                                                 ConnectionFlags.CompressionOptions.Optimal,
                CompressionStrategies.Fastest => ConnectionFlags.CompressionOptions.Brotli |
                                                 ConnectionFlags.CompressionOptions.Fastest,
                CompressionStrategies.SmallestSize => ConnectionFlags.CompressionOptions.Brotli |
                                                      ConnectionFlags.CompressionOptions.SmallestSize,
                _ => throw new ArgumentOutOfRangeException()
            },
            CompressionAlgorithms.ZLib => Strategy switch
            {
                CompressionStrategies.Optimal => ConnectionFlags.CompressionOptions.ZLib |
                                                 ConnectionFlags.CompressionOptions.Optimal,
                CompressionStrategies.Fastest => ConnectionFlags.CompressionOptions.ZLib |
                                                 ConnectionFlags.CompressionOptions.Fastest,
                CompressionStrategies.SmallestSize => ConnectionFlags.CompressionOptions.ZLib |
                                                      ConnectionFlags.CompressionOptions.SmallestSize,
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    private static async Task<Dictionary<CompressionOptions, (TcpServer, Task, AstraClient)>> CreateTable()
    {
        var table = new Dictionary<CompressionOptions, (TcpServer, Task, AstraClient)>();
        var port = TcpServer.DefaultPort;
        foreach (var option in Enum.GetValues(typeof(CompressionOptions)).Cast<CompressionOptions>())
        {
            if (option == CompressionOptions.None) continue;
            var currPort = port++;
            var server = new TcpServer(new()
            {
                Port = currPort,
                LogLevel = "Critical",
                UseCellBasedDataStore = true,
                Schema = Schema with { BinaryTreeDegree = 1_000 },
                CompressionOption = option
            }, AuthenticationHelper.NoAuthentication());
            var serverTask = server.RunAsync();
            await Task.Delay(5);
            var client = new AstraClient();
            await client.ConnectAsync(new()
            {
                Address = "127.0.0.1",
                Port = currPort,
            });
            table[option] = (server, serverTask, client);
        }

        return table;
    }

    private async Task GlobalSetUpAsync()
    {
        _lookupTable = await CreateTable();
        _plan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(Index).Build();
    }

    private async Task GlobalCleanUpAsync()
    {
        foreach (var (_, (server, task, client)) in _lookupTable)
        {
            client.Dispose();
            server.Kill();
            await task;
            server.Dispose();
        }

        _lookupTable = null!;
        
    }

    [GlobalSetup]
    public void GlobalSetUp() => GlobalSetUpAsync().Wait();

    [GlobalCleanup]
    public void GlobalCleanUp() => GlobalCleanUpAsync().Wait();
    
    private async Task IterationSetupAsync()
    {
        var (_, _, client) = _lookupTable[SelectCompressionOption()];
        var data = new SimpleSerializableStruct[AggregatedRows + GibberishRows];
        for (var i = 0; i < AggregatedRows; i++)
        {
            data[i] = new()
            {
                Value1 = Index, // 4 bytes
                Value2 = "test", // 4 + 4 bytes
                Value3 = i.ToString() // 4 + (<= 4) bytes
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

        await client.BulkInsertSerializableCompatAsync(data);
    }
    
    [IterationSetup]
    public void IterationSetUp()
    {
        IterationSetupAsync().Wait();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        var (server, _, _) = _lookupTable[SelectCompressionOption()];
        server.GetRegistry().Clear();
    }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ProfessionalTimeWaster<T>(T _) {  }
    
    private async Task SimpleAggregationAndAutoDeserializationBenchmarkNewAsync()
    {
        var (_, _, client) = _lookupTable[SelectCompressionOption()];
        using var fetched = await client.AggregateAsync<SimpleSerializableStruct>(_plan);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    [Benchmark]
    public void AutoDeserializationNew()
    {
        SimpleAggregationAndAutoDeserializationBenchmarkNewAsync().Wait();
    }
}