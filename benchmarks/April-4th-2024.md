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

- ForwardIterationBenchmark

| Method        | RecordCount | StringLength | Mean         | Error      | StdDev     | Median       |
|-------------- |------------ |------------- |-------------:|-----------:|-----------:|-------------:|
| Astra         | 1000        | 0            |  1,890.22 us | 275.224 us | 771.757 us |  1,537.51 us |
| AmortizedList | 1000        | 0            |     24.64 us |   1.304 us |   3.525 us |     25.00 us |
| HashMap       | 1000        | 0            |     30.12 us |   3.485 us |  10.167 us |     25.11 us |
| RbTree        | 1000        | 0            |    102.63 us |   9.482 us |  27.358 us |    107.06 us |
| Astra         | 1000        | 1000         |  2,728.84 us | 223.666 us | 623.492 us |  2,640.34 us |
| AmortizedList | 1000        | 1000         |     24.60 us |   1.967 us |   5.612 us |     23.77 us |
| HashMap       | 1000        | 1000         |     33.67 us |   3.004 us |   8.762 us |     36.57 us |
| RbTree        | 1000        | 1000         |     88.39 us |  11.504 us |  33.558 us |    100.62 us |
| Astra         | 10000       | 0            |  6,469.69 us | 127.562 us | 282.670 us |  6,437.56 us |
| AmortizedList | 10000       | 0            |     34.78 us |   3.859 us |  11.256 us |     41.69 us |
| HashMap       | 10000       | 0            |     82.85 us |   1.477 us |   1.309 us |     82.61 us |
| RbTree        | 10000       | 0            |    644.07 us |  33.785 us |  94.738 us |    634.98 us |
| Astra         | 10000       | 1000         | 19,445.10 us | 376.796 us | 476.526 us | 19,554.83 us |
| AmortizedList | 10000       | 1000         |     42.02 us |   4.826 us |  13.924 us |     45.72 us |
| HashMap       | 10000       | 1000         |     93.37 us |   6.415 us |  18.612 us |     89.79 us |
| RbTree        | 10000       | 1000         |  1,030.97 us |  53.024 us | 155.509 us |    983.13 us |


 - PointQueryBenchmark

 | Method        | RecordCount | StringLength | Mean       | Error      | StdDev     | Median     |
|-------------- |------------ |------------- |-----------:|-----------:|-----------:|-----------:|
| Astra         | 1000        | 0            | 151.780 us |  8.9704 us | 26.1672 us | 152.351 us |
| AmortizedList | 1000        | 0            |  67.276 us |  1.6570 us |  4.6190 us |  67.365 us |
| HashMap       | 1000        | 0            |   4.574 us |  0.2111 us |  0.6022 us |   4.521 us |
| RbTree        | 1000        | 0            |   4.632 us |  0.3293 us |  0.9606 us |   4.618 us |
| Astra         | 1000        | 1000         | 149.039 us |  6.9027 us | 19.8051 us | 148.098 us |
| AmortizedList | 1000        | 1000         |  67.690 us |  2.5498 us |  7.4782 us |  66.799 us |
| HashMap       | 1000        | 1000         |   4.786 us |  0.2426 us |  0.6999 us |   4.814 us |
| RbTree        | 1000        | 1000         |   5.208 us |  0.3724 us |  1.0745 us |   5.157 us |
| Astra         | 10000       | 0            | 113.957 us |  8.5026 us | 24.5319 us | 110.593 us |
| AmortizedList | 10000       | 0            | 109.441 us |  2.0467 us |  1.8144 us | 108.949 us |
| HashMap       | 10000       | 0            |   4.067 us |  0.8010 us |  2.3617 us |   4.317 us |
| RbTree        | 10000       | 0            |   5.457 us |  0.5383 us |  1.5788 us |   4.815 us |
| Astra         | 10000       | 1000         | 125.408 us |  6.5684 us | 18.8460 us | 123.418 us |
| AmortizedList | 10000       | 1000         |  90.967 us | 11.3082 us | 33.1649 us | 113.191 us |
| HashMap       | 10000       | 1000         |   4.162 us |  0.7962 us |  2.3352 us |   4.471 us |
| RbTree        | 10000       | 1000         |   5.738 us |  0.6004 us |  1.7419 us |   4.954 us |
