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

| Method                | AggregatedRows | Mean        | Error       | StdDev      | Median      |
|---------------------- |--------------- |------------:|------------:|------------:|------------:|
| ManualDeserialization | 100            |    294.1 us |    27.09 us |    79.46 us |    281.3 us |
| AutoDeserialization   | 100            |    285.9 us |    26.53 us |    77.82 us |    281.8 us |
| ManualDeserialization | 1000           |  1,347.3 us |   271.58 us |   800.75 us |    798.2 us |
| AutoDeserialization   | 1000           |    840.7 us |    76.14 us |   207.14 us |    751.2 us |
| ManualDeserialization | 10000          |  5,637.3 us |   111.13 us |   185.68 us |  5,631.5 us |
| AutoDeserialization   | 10000          |  5,594.0 us |   100.00 us |    83.50 us |  5,602.2 us |
| ManualDeserialization | 100000         | 63,965.3 us | 1,273.56 us | 1,826.50 us | 63,484.1 us |
| AutoDeserialization   | 100000         | 68,661.1 us | 1,340.98 us | 1,490.50 us | 68,491.8 us |


- NetworkAggregationBenchmark:

| Method                | AggregatedRows | Mean     | Error    | StdDev   | Median   |
|---------------------- |--------------- |---------:|---------:|---------:|---------:|
| Transmission          | 100            | 42.19 ms | 0.839 ms | 1.513 ms | 41.36 ms |
| ManualDeserialization | 100            | 40.74 ms | 0.173 ms | 0.135 ms | 40.78 ms |
| AutoDeserialization   | 100            | 42.94 ms | 0.853 ms | 1.800 ms | 41.92 ms |
| Transmission          | 1000           | 42.49 ms | 0.820 ms | 1.066 ms | 42.55 ms |
| ManualDeserialization | 1000           | 45.13 ms | 0.777 ms | 0.649 ms | 45.19 ms |
| AutoDeserialization   | 1000           | 44.99 ms | 0.437 ms | 0.365 ms | 44.97 ms |
| Transmission          | 10000          | 42.11 ms | 0.837 ms | 0.930 ms | 42.35 ms |
| ManualDeserialization | 10000          | 60.83 ms | 2.715 ms | 7.919 ms | 62.81 ms |
| AutoDeserialization   | 10000          | 39.49 ms | 0.663 ms | 0.554 ms | 39.41 ms |
