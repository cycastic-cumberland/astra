using BenchmarkDotNet.Running;

namespace Astra.Benchmark;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}