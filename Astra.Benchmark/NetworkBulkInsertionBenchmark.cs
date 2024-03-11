using System.Security.Cryptography;
using Astra.Client.Simple;
using Astra.Common.Data;
using Astra.Common.Hashes;
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
    private SimpleAstraClient _client = null!;
    private Task _serverTask = Task.CompletedTask;
    private SimpleSerializableStruct[] _array = null!;
    private const uint MaxBulkInsertAmount = 10_000;
    
    [Params(10, 100, 1_000, MaxBulkInsertAmount)]
    public uint BulkInsertAmount;

    public async Task GlobalSetupAsync()
    {
        _server = new(new()
        {
            LogLevel = "Critical",
            Schema = Schema with { BinaryTreeDegree = (int)(BulkInsertAmount / 10) }
        }, AuthenticationHelper.NoAuthentication());
        _serverTask = _server.RunAsync();
        await Task.Delay(100);
        _client = new();
        await _client.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
        });
    }
    
    public Task GlobalCleanupAsync()
    {
        _client.Dispose();
        _server.Kill();
        _server = null!;
        return _serverTask;
    }
    
    [GlobalSetup]
    public void FirstSetup()
    {
        GlobalSetupAsync().Wait();
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
    }
        
    [Benchmark]
    public void BulkInsertionBenchmark()
    { 
        _client.BulkInsertSerializableCompatAsync(_array).Wait();
    }
}
