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
| BulkInsertionBenchmark | 10               |     54.68 us |   1.608 us |     4.561 us |     53.77 us |
| BulkInsertionBenchmark | 100              |    546.46 us |  37.468 us |   107.504 us |    517.97 us |
| BulkInsertionBenchmark | 1000             |  2,307.32 us | 408.688 us | 1,139.260 us |  1,741.34 us |
| BulkInsertionBenchmark | 10000            | 16,534.76 us | 330.394 us |   921.007 us | 16,496.62 us |



- NetworkBulkInsertionBenchmark:

| Method                 | BulkInsertAmount | Mean        | Error     | StdDev      | Median      |
|----------------------- |----------------- |------------:|----------:|------------:|------------:|
| BulkInsertionBenchmark | 10               |    146.4 us |   9.14 us |    26.24 us |    143.5 us |
| BulkInsertionBenchmark | 100              |    973.4 us |  56.59 us |   161.46 us |    957.9 us |
| BulkInsertionBenchmark | 1000             |  3,894.9 us | 168.13 us |   445.86 us |  3,733.3 us |
| BulkInsertionBenchmark | 10000            | 34,510.9 us | 678.78 us | 1,323.90 us | 34,326.4 us |


- LocalAggregationBenchmark:

| Method                                       | AggregatedRows | Mean        | Error     | StdDev    | Median      |
|--------------------------------------------- |--------------- |------------:|----------:|----------:|------------:|
| SimpleAggregationAndDeserializationBenchmark | 100            |    291.2 us |  24.90 us |  73.02 us |    297.6 us |
| SimpleAggregationAndDeserializationBenchmark | 1000           |    910.1 us | 131.40 us | 364.12 us |    705.8 us |
| SimpleAggregationAndDeserializationBenchmark | 10000          |  5,733.5 us | 112.64 us | 120.52 us |  5,746.3 us |
| SimpleAggregationAndDeserializationBenchmark | 100000         | 63,230.0 us | 966.58 us | 904.14 us | 63,521.0 us |


- NetworkAggregationBenchmark:

| Method                                       | AggregatedRows | Mean     | Error    | StdDev   | Median   |
|--------------------------------------------- |--------------- |---------:|---------:|---------:|---------:|
| TransmissionBenchmark                        | 100            | 41.04 ms | 0.270 ms | 0.240 ms | 41.05 ms |
| SimpleAggregationAndDeserializationBenchmark | 100            | 41.74 ms | 0.679 ms | 0.974 ms | 41.46 ms |
| TransmissionBenchmark                        | 1000           | 43.72 ms | 0.866 ms | 1.749 ms | 43.15 ms |
| SimpleAggregationAndDeserializationBenchmark | 1000           | 47.31 ms | 0.938 ms | 1.916 ms | 46.16 ms |
| TransmissionBenchmark                        | 10000          | 42.13 ms | 0.815 ms | 1.269 ms | 42.10 ms |
| SimpleAggregationAndDeserializationBenchmark | 10000          | 35.58 ms | 0.340 ms | 0.847 ms | 35.33 ms |


