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

| Method              | BulkInsertAmount | Mean         | Error      | StdDev       | Median       |
|-------------------- |----------------- |-------------:|-----------:|-------------:|-------------:|
| ManualSerialization | 10               |     57.54 us |   1.870 us |     5.276 us |     56.90 us |
| AutoSerialization   | 10               |     55.06 us |   1.957 us |     5.551 us |     53.60 us |
| ManualSerialization | 100              |    590.93 us |  53.241 us |   155.307 us |    532.12 us |
| AutoSerialization   | 100              |    646.53 us |  66.435 us |   191.680 us |    563.19 us |
| ManualSerialization | 1000             |  2,032.27 us | 267.561 us |   718.785 us |  1,736.04 us |
| AutoSerialization   | 1000             |  2,821.42 us | 574.253 us | 1,684.184 us |  1,848.76 us |
| ManualSerialization | 10000            | 17,145.48 us | 379.205 us | 1,100.143 us | 17,236.74 us |
| AutoSerialization   | 10000            | 17,258.24 us | 341.185 us |   783.930 us | 17,414.99 us |

- NetworkBulkInsertionBenchmark:

| Method              | BulkInsertAmount | Mean         | Error        | StdDev        | Median       |
|-------------------- |----------------- |-------------:|-------------:|--------------:|-------------:|
| ManualSerialization | 10               |    110.04 us |     6.439 us |     18.577 us |    107.91 us |
| AutoSerialization   | 10               |     99.29 us |     4.389 us |     12.234 us |     95.04 us |
| ManualSerialization | 100              |    555.39 us |    13.577 us |     38.955 us |    540.79 us |
| AutoSerialization   | 100              |    547.77 us |     7.388 us |      6.910 us |    546.47 us |
| ManualSerialization | 1000             | 25,371.53 us | 6,535.718 us | 19,270.711 us | 40,765.60 us |
| AutoSerialization   | 1000             |  5,568.84 us |   107.193 us |    153.733 us |  5,563.28 us |
| ManualSerialization | 10000            | 31,308.70 us |   541.031 us |    451.785 us | 31,211.32 us |
| AutoSerialization   | 10000            | 30,158.39 us |   588.044 us |  1,075.272 us | 30,186.17 us |


- LocalAggregationBenchmark:

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
| Transmission          | 100            | 42.57 ms | 0.843 ms | 1.778 ms | 41.52 ms |
| ManualDeserialization | 100            | 42.64 ms | 0.847 ms | 1.786 ms | 41.48 ms |
| AutoDeserialization   | 100            | 42.73 ms | 0.849 ms | 1.917 ms | 42.18 ms |
| Transmission          | 1000           | 42.52 ms | 0.837 ms | 1.145 ms | 42.94 ms |
| ManualDeserialization | 1000           | 46.52 ms | 0.879 ms | 0.977 ms | 46.22 ms |
| AutoDeserialization   | 1000           | 46.92 ms | 0.938 ms | 1.567 ms | 46.41 ms |
| Transmission          | 10000          | 42.07 ms | 0.817 ms | 0.683 ms | 42.42 ms |
| ManualDeserialization | 10000          | 63.99 ms | 1.277 ms | 3.539 ms | 63.66 ms |
| AutoDeserialization   | 10000          | 37.46 ms | 0.737 ms | 1.231 ms | 37.07 ms |
