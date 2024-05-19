using System.Security.Cryptography;
using Astra.Client;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Serializable;
using Astra.Server;
using Astra.Server.Authentication;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class NetworkBulkInsertionBenchmark
{
    private static readonly RegistrySchemaSpecifications Schema = new()
    {
        Columns =
        [
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
        ]
    };

    private TcpServer _server = null!;
    private TcpServer _newServer = null!;
    private AstraClient _client = null!;
    private AstraClient _client2 = null!;
    private Task _serverTask = Task.CompletedTask;
    private Task _newServerTask = Task.CompletedTask;
    private SimpleSerializableStruct[] _array = null!;
    private const uint MaxBulkInsertAmount = 10_000;
    
    [Params(10, 100, 1_000, MaxBulkInsertAmount)]
    public uint BulkInsertAmount;

    public async Task GlobalSetupAsync()
    {
        _server = new(new()
        {
            Port = TcpServer.DefaultPort,
            LogLevel = "Critical",
            UseCellBasedDataStore = false,
            Schema = Schema with { BinaryTreeDegree = (int)(BulkInsertAmount / 10) }
        }, AuthenticationHelper.NoAuthentication());
        _newServer = new(new()
        {
            Port = TcpServer.DefaultPort + 1,
            LogLevel = "Critical",
            UseCellBasedDataStore = true,
            Schema = Schema with { BinaryTreeDegree = (int)(BulkInsertAmount / 10) }
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
    public void FirstSetup()
    {
        GlobalSetupAsync().Wait();
        DynamicSerializable.EnsureBuilt<SimpleSerializableStruct>();
    }
    
    [GlobalCleanup]
    public void LastCleanUp()
    {
        GlobalCleanupAsync().Wait();
    }

    private uint _stuff;
    
    [IterationSetup]
    public void Setup()
    {
        var stuff = Interlocked.Increment(ref _stuff);
        _array = new SimpleSerializableStruct[BulkInsertAmount];
        for (var i = 0U; i < BulkInsertAmount; i++)
        {
            _array[i] = new()
            {
                Value1 = unchecked((int)stuff),
                Value2 = "test",
                Value3 = Hash128.CreateUnsafe(RandomNumberGenerator.GetBytes(Hash128.Size)).ToStringUpperCase()
            };
        }
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _array = null!;
        _server.GetRegistry().Clear();
        _newServer.GetRegistry().Clear();
    }
        
    [Benchmark]
    public void ManualSerialization()
    { 
        _client.BulkInsertSerializableCompatAsync(_array).Wait();
    }
    
    [Benchmark]
    public void AutoSerialization()
    { 
        _client.BulkInsertAsync(_array).Wait();
    }
    
    [Benchmark]
    public void ManualSerializationNew()
    { 
        _client2.BulkInsertSerializableCompatAsync(_array).Wait();
    }
    
    [Benchmark]
    public void AutoSerializationNew()
    { 
        _client2.BulkInsertAsync(_array).Wait();
    }
}
