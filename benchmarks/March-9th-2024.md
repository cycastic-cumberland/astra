```
// * Summary *
AggregatedRows
BenchmarkDotNet v0.13.11, Arch Linux
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
  [Host]   : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 8.0 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 8.0  Runtime=.NET 8.0  InvocationCount=1  
UnrollFactor=1 
```

- LocalBulkInsertionBenchmark:

| Method                 | BulkInsertAmount | Mean         | Error      | StdDev       | Median       |
|----------------------- |----------------- |-------------:|-----------:|-------------:|-------------:|
| BulkInsertionBenchmark | 10               |     72.18 us |   3.374 us |     9.405 us |     72.10 us |
| BulkInsertionBenchmark | 100              |    664.61 us |  83.745 us |   241.624 us |    535.72 us |
| BulkInsertionBenchmark | 1000             |  1,988.63 us | 225.062 us |   608.467 us |  1,769.58 us |
| BulkInsertionBenchmark | 10000            | 17,671.41 us | 519.706 us | 1,457.310 us | 17,403.87 us |


- NetworkBulkInsertionBenchmark:

| Method                 | BulkInsertAmount | Mean        | Error     | StdDev      | Median      |
|----------------------- |----------------- |------------:|----------:|------------:|------------:|
| BulkInsertionBenchmark | 10               |    146.4 us |   9.14 us |    26.24 us |    143.5 us |
| BulkInsertionBenchmark | 100              |    973.4 us |  56.59 us |   161.46 us |    957.9 us |
| BulkInsertionBenchmark | 1000             |  3,894.9 us | 168.13 us |   445.86 us |  3,733.3 us |
| BulkInsertionBenchmark | 10000            | 34,510.9 us | 678.78 us | 1,323.90 us | 34,326.4 us |


- LocalAggregationBenchmark:

| Method                                       | AggregatedRows | Mean       | Error     | StdDev    | Median     |
|--------------------------------------------- |--------------- |-----------:|----------:|----------:|-----------:|
| SimpleAggregationAndDeserializationBenchmark | 100            |   294.4 us |  24.40 us |  71.93 us |   318.5 us |
| SimpleAggregationAndDeserializationBenchmark | 1000           | 1,372.6 us | 274.38 us | 804.70 us | 1,029.1 us |
| SimpleAggregationAndDeserializationBenchmark | 10000          | 5,530.5 us | 110.17 us | 108.20 us | 5,503.1 us |


- NetworkAggregationBenchmark:

| Method                                       | AggregatedRows | Mean     | Error    | StdDev   | Median   |
|--------------------------------------------- |--------------- |---------:|---------:|---------:|---------:|
| TransmissionBenchmark                        | 100            | 41.04 ms | 0.270 ms | 0.240 ms | 41.05 ms |
| SimpleAggregationAndDeserializationBenchmark | 100            | 41.74 ms | 0.679 ms | 0.974 ms | 41.46 ms |
| TransmissionBenchmark                        | 1000           | 43.72 ms | 0.866 ms | 1.749 ms | 43.15 ms |
| SimpleAggregationAndDeserializationBenchmark | 1000           | 47.31 ms | 0.938 ms | 1.916 ms | 46.16 ms |
| TransmissionBenchmark                        | 10000          | 42.13 ms | 0.815 ms | 1.269 ms | 42.10 ms |
| SimpleAggregationAndDeserializationBenchmark | 10000          | 35.58 ms | 0.340 ms | 0.847 ms | 35.33 ms |


