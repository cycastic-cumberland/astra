# Astra
 
Astra is a lightweight, tabular database for fast and effective caching of structured data.

## Requirement

Requires .NET 8 for both client and server (there is no good reason for this, I just don't know how to install .NET 6 on Arch Linux)

## Getting Started

To use Astra in TCP server mode, clone this repo and build `Astra.Server` 

## Roadmap

- [x] Embeddable engine
- [x] TCP server
- [x] Working client
- [ ] ORM
- [ ] More feature?
- [ ] Fix more bugs?
- [ ] Run faster?

## Benchmark results

```
// * Summary *

BenchmarkDotNet v0.13.11, Arch Linux
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.100
  [Host]   : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  .NET 8.0 : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET 8.0  Runtime=.NET 8.0  InvocationCount=1  
UnrollFactor=1 

// * Legends *
  BulkInsertAmount : Value of the 'BulkInsertAmount' parameter
  Mean             : Arithmetic mean of all measurements
  Error            : Half of 99.9% confidence interval
  StdDev           : Standard deviation of all measurements
  Median           : Value separating the higher half of all measurements (50th percentile)
  1 us             : 1 Microsecond (0.000001 sec)
  1 ms             : 1 Millisecond (0.001 sec)
```

- LocalBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |         Mean |      Error |       StdDev |       Median |
|:-----------------------|-----------------:|-------------:|-----------:|-------------:|-------------:|
| BulkInsertionBenchmark |               10 |     44.96 us |   1.410 us |     4.090 us |     44.03 us |
| BulkInsertionBenchmark |              100 |    378.67 us |  26.731 us |    77.125 us |    341.89 us |
| BulkInsertionBenchmark |             1000 |  3,470.49 us | 478.137 us | 1,394.749 us |  3,150.87 us |
| BulkInsertionBenchmark |            10000 | 26,186.84 us | 531.090 us | 1,515.230 us | 26,361.57 us |

- NetworkBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |      Mean |    Error |    StdDev |    Median |
|:-----------------------|-----------------:|----------:|---------:|----------:|----------:|
| BulkInsertionBenchmark |               10 | 86.685 ms | 1.657 ms |  1.701 ms | 85.600 ms |
| BulkInsertionBenchmark |              100 | 86.007 ms | 1.707 ms |  2.033 ms | 85.137 ms |
| BulkInsertionBenchmark |             1000 | 22.112 ms | 7.479 ms | 22.051 ms |  3.990 ms |
| BulkInsertionBenchmark |             2000 |  8.818 ms | 1.466 ms |  3.862 ms |  7.467 ms |

(My computer just couldn't process bulk inserting 10 thousand rows over TCP)
