using System.Security.Cryptography;
using Astra.Client;
using Astra.Engine;
using Astra.Server;
using Astra.Server.Authentication;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class NetworkBulkInsertionBenchmark
{
    private static readonly SchemaSpecifications Schema = new()
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
    };

    private TcpServer _server = null!;
    private SimpleAstraClient _client = null!;
    private Task _serverTask = Task.CompletedTask;
    private SimpleSerializableStruct[] _array = null!;
    
    [Params(10, 100, 1_000, 2_000)]
    public uint BulkInsertAmount;

    public async Task GlobalSetupAsync()
    {
        _server = new(new()
        {
            LogLevel = "Critical",
            Schema = Schema
        }, AuthenticationHelper.NoAuthentication());
        _serverTask = _server.RunAsync();
        await Task.Delay(100);
        _client = new();
        await _client.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort,
            Schema = Schema
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

    [IterationSetup]
    public void Setup()
    {
        _array = new SimpleSerializableStruct[BulkInsertAmount];
        for (var i = 0U; i < BulkInsertAmount; i++)
        {
            _array[i] = new()
            {
                Value1 = unchecked((int)i),
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
        _client.BulkInsertSerializableAsync(_array).Wait();
    }
}
