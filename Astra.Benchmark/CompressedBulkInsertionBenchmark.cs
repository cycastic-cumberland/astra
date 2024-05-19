using System.Security.Cryptography;
using Astra.Client;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Server;
using Astra.Server.Authentication;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class CompressedBulkInsertionBenchmark
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

    private TcpServer _newServer = null!;
    private AstraClient _client2 = null!;
    private Task _newServerTask = Task.CompletedTask;
    private SimpleSerializableStruct[] _array = null!;
    private const uint MaxBulkInsertAmount = 10_000;
    
    [Params(MaxBulkInsertAmount)]
    public uint BulkInsertAmount;

    [Params(CompressionAlgorithms.GZip, CompressionAlgorithms.Deflate, CompressionAlgorithms.Brotli, CompressionAlgorithms.ZLib)]
    public CompressionAlgorithms Algorithm;
    
    [Params(CompressionStrategies.Optimal, CompressionStrategies.Fastest, CompressionStrategies.SmallestSize)]
    public CompressionStrategies Strategy;

    private uint _stuff;

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
    
    private async Task SetUpAsync()
    {
        _newServer = new(new()
        {
            Port = TcpServer.DefaultPort + 1,
            LogLevel = "Critical",
            UseCellBasedDataStore = true,
            Schema = Schema with { BinaryTreeDegree = (int)(BulkInsertAmount / 10) },
            CompressionOption = SelectCompressionOption()
        }, AuthenticationHelper.NoAuthentication());
        _newServerTask = _newServer.RunAsync();
        await Task.Delay(100);
        _client2 = new();
        
        await _client2.ConnectAsync(new()
        {
            Address = "127.0.0.1",
            Port = TcpServer.DefaultPort + 1,
        });
    }
    
    [IterationSetup]
    public void Setup()
    {
        SetUpAsync().Wait();
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

    private Task CleanUpAsync()
    {
        _client2.Dispose();
        _newServer.Kill();
        _newServer = null!;
        return _newServerTask;
    }

    [IterationCleanup]
    public void CleanUp()
    {
        _array = null!;
        _newServer.GetRegistry().Clear();
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