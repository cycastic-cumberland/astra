using System.Security.Cryptography;
using Astra.Common;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Astra.Benchmark;

[SimpleJob(RuntimeMoniker.Net80)]
public class HashFunctionsBenchmark
{
    private RandomNumberGenerator _rng = null!;
    private BytesCluster _bytes;

    private Hash128 _hash128;
    private Hash256 _hash256;
    private Hash512 _hash512;
#pragma warning disable CS0169
    private bool _result;
#pragma warning restore CS0169

    [Params(1024, 4096)]
    public int DataLength;

    [Params(8192)]
    public int RepeatCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _rng = RandomNumberGenerator.Create();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _rng.Dispose();
    }

    [IterationSetup]
    public void Setup()
    {
        _bytes = BytesCluster.Rent(DataLength);
        _rng.GetBytes(_bytes.Writer);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _bytes.Dispose();
        _bytes = default;
    }

    [Benchmark]
    public void HashXxHash3()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            _hash128 = Hash128.HashXx128(_bytes.Reader);
        }
    }
    
    [Benchmark]
    public void HashSha256()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            _hash256 = Hash256.HashSha256(_bytes.Reader);
        }
    }
    
    [Benchmark]
    public void HashBlake2B()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            _hash512 = Hash512.HashBlake2B(_bytes.Reader);
        }
    }
    
    [Benchmark]
    public void HashSha512()
    {
        for (var i = 0; i < RepeatCount; i++)
        {
            _hash512 = Hash512.HashSha512(_bytes.Reader);
        }
    }
}