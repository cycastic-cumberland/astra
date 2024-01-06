using BenchmarkDotNet.Running;

namespace Astra.Benchmark;

public static class Program
{
    public static Task Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args);
        return Task.CompletedTask;
    }
}