using System.Security.Cryptography;
using Astra.Client;
using Astra.Common.Hashes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class DetachedBulkInsertionBenchmark
{
    public const int Port = 8499;
    private AstraClient _client = null!;
    private SimpleSerializableStruct[] _array = null!;
    
    [Params(1_000, 10_000, 100_000)]
    public uint BulkInsertAmount;

    private int _id;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Task.Run(async () =>
        {
            _client = new();
        
            await _client.ConnectAsync(new()
            {
                Address = "127.0.0.1",
                Port = Port,
            });
        }).Wait();
    }
    
    [IterationSetup]
    public void Setup()
    {
        var stuff = Interlocked.Increment(ref _id);
        _array = new SimpleSerializableStruct[BulkInsertAmount];
        for (var i = 0U; i < BulkInsertAmount; i++)
        {
            _array[i] = new()
            {
                Value1 = stuff,
                Value2 = "test",
                Value3 = Hash128.CreateUnsafe(RandomNumberGenerator.GetBytes(Hash128.Size)).ToStringUpperCase()
            };
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _client.ClearAsync().Wait();
        _array = null!;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _client.Dispose();
        _client = null!;
    }
    
    [Benchmark]
    public void AutoSerializationNew()
    { 
        _client.BulkInsertAsync(_array).Wait();
    }
}