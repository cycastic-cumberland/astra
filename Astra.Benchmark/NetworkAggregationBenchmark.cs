using Astra.Client.Simple;
using Astra.Client.Simple.Aggregator;
using Astra.Common.Data;
using Astra.Server;
using Astra.Server.Authentication;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class NetworkAggregationBenchmark
{
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
    
    private readonly AstraTable<int, string, string> _table = new();
    private TcpServer _server = null!;
    private SimpleAstraClient _client = null!;
    private Task _serverTask = Task.CompletedTask;
    private GenericAstraQueryBranch _predicate;
    private GenericAstraQueryBranch _fakePredicate;
    
    public async Task GlobalSetupAsync()
    {
        _server = new(new()
        {
            LogLevel = "Critical",
            Schema = Schema with { BinaryTreeDegree = 10_000 }
        }, AuthenticationHelper.NoAuthentication());
        _serverTask = _server.RunAsync();
        await Task.Delay(100);
        _client = new();
        await _client.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
        });
        _predicate = _table.Column1.EqualsLiteral(Index);
        _fakePredicate = _table.Column1.EqualsLiteral(-Index);
    }
    
    public Task GlobalCleanupAsync()
    {
        _client.Dispose();
        _server.Kill();
        _server = null!;
        return _serverTask;
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
    
    [Params(100, 1_000, 10_000)]
    public uint AggregatedRows;
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
    }

    private Task IterationCleanupAsync() => _client.ClearAsync();

    [IterationSetup]
    public void IterationSetUp()
    {
        IterationSetupAsync().Wait();
    }
    
    private ulong _a;

    [IterationCleanup]
    public void IterationCleanup()
    {
        IterationCleanupAsync().Wait();
    }

    
    private async Task TransmissionBenchmarkAsync()
    {
        using var a = await _client.AggregateCompatAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_fakePredicate);
    }

    private async Task SimpleAggregationAndManualDeserializationBenchmarkAsync()
    {
        using var fetched = await _client.AggregateCompatAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_predicate);
        foreach (var f in fetched)
        {
            _a += unchecked((ulong)f.Value1);
        }
    }

    private async Task SimpleAggregationAndAutoDeserializationBenchmarkAsync()
    {
        using var fetched = await _client.AggregateAsync<SimpleSerializableStruct, GenericAstraQueryBranch>(_predicate);
        foreach (var f in fetched)
        {
            _a += unchecked((ulong)f.Value1);
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
}