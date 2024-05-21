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
public class NetworkAggregationBenchmark
{
    [Params(100, 1_000)]
    public uint AggregatedRows;
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
    
    private TcpServer _server = null!;
    private TcpServer _newServer = null!;
    private AstraClient _client = null!;
    private AstraClient _client2 = null!;
    private Task _serverTask = Task.CompletedTask;
    private Task _newServerTask = Task.CompletedTask;
    private GenericAstraQueryBranch _predicate;
    private PhysicalPlan _plan;
    private GenericAstraQueryBranch _fakePredicate;
    private PhysicalPlan _fakePlan;
    
    public async Task GlobalSetupAsync()
    {
        _server = new(new()
        {
            Port = TcpServer.DefaultPort,
            LogLevel = "Critical",
            UseCellBasedDataStore = false,
            Schema = Schema with { BinaryTreeDegree = 1_000 }
        }, AuthenticationHelper.NoAuthentication());
        _newServer = new(new()
        {
            Port = TcpServer.DefaultPort + 1,
            LogLevel = "Critical",
            UseCellBasedDataStore = true,
            Schema = Schema with { BinaryTreeDegree = 1_000 }
        }, AuthenticationHelper.NoAuthentication());
        _serverTask = _server.RunAsync();
        _newServerTask = _newServer.RunAsync();
        await Task.Delay(100);
        _client = new();
        await _client.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
        });
        _client2 = new();
        await _client2.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort + 1,
        });
        _predicate = AstraTable<int, string, string>.Column1.EqualsLiteral(Index);
        _plan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(Index).Build();
        _fakePredicate = AstraTable<int, string, string>.Column1.EqualsLiteral(-Index);
        _fakePlan = PhysicalPlanBuilder.Column<int>(0).EqualsTo(-Index).Build();
    }
    
    public Task GlobalCleanupAsync()
    {
        _client.Dispose();
        _client2.Dispose();
        _server.Kill();
        _server = null!;
        _newServer.Kill();
        _newServer = null!;
        return Task.WhenAll(_serverTask, _newServerTask);
    }
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        GlobalSetupAsync().Wait();
    }
    
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        GlobalCleanupAsync().Wait();
    }
    private uint GibberishRows => AggregatedRows / 2;
    
    private const int Index = 1;
    
    private async Task IterationSetupAsync()
    {
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

        await _client.BulkInsertSerializableCompatAsync(data);
        await _client2.BulkInsertSerializableCompatAsync(data);
    }


    [IterationSetup]
    public void IterationSetUp()
    {
        IterationSetupAsync().Wait();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _server.GetRegistry().Clear();
        // _client2.ClearAsync().Wait();
        _newServer.GetRegistry().Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ProfessionalTimeWaster<T>(T _) {  }
    
    
    private async Task TransmissionBenchmarkAsync()
    {
        using var a = await _client.AggregateCompatAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_fakePredicate);
    }

    private async Task SimpleAggregationAndManualDeserializationBenchmarkAsync()
    {
        using var fetched = await _client.AggregateCompatAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_predicate);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }

    private async Task SimpleAggregationAndAutoDeserializationBenchmarkAsync()
    {
        using var fetched = await _client.AggregateAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_predicate);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }
    
    private async Task TransmissionBenchmarkNewAsync()
    {
        using var a = await _client2.AggregateAsync<SimpleSerializableStruct>(_fakePlan);
    }
    
    private async Task SimpleAggregationAndManualDeserializationBenchmarkNewAsync()
    {
        using var fetched = await _client2.AggregateCompatAsync<SimpleSerializableStruct>(_predicate);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }

    private async Task SimpleAggregationAndAutoDeserializationBenchmarkNewAsync()
    {
        using var fetched = await _client2.AggregateAsync<SimpleSerializableStruct>(_plan);
        foreach (var f in fetched)
        {
            ProfessionalTimeWaster(f.Value1);
        }
    }

    [Benchmark]
    public void Transmission()
    {
        TransmissionBenchmarkAsync().Wait();
    }

    [Benchmark]
    public void ManualDeserialization()
    {
        SimpleAggregationAndManualDeserializationBenchmarkAsync().Wait();
    }
    
    [Benchmark]
    public void AutoDeserialization()
    {
        SimpleAggregationAndAutoDeserializationBenchmarkAsync().Wait();
    }
    
    [Benchmark]
    public void TransmissionNew()
    {
        TransmissionBenchmarkNewAsync().Wait();
    }
    
    [Benchmark]
    public void ManualDeserializationNew()
    {
        SimpleAggregationAndManualDeserializationBenchmarkNewAsync().Wait();
    }
    
    [Benchmark]
    public void AutoDeserializationNew()
    {
        SimpleAggregationAndAutoDeserializationBenchmarkNewAsync().Wait();
    }
}