# Astra
 
Astra is a lightweight, tabular database for fast and effective caching of structured data.

## Requirements

(Currently) requires .NET 8 for both client and server.
## Motivation

The motivation behind Astra stems from my need for a cache server that can not only stores and aggregates data but also allows for the conditional deletion of multiple "unit of data". 
First, I chose Redis for its speed; however, it fell short in fulfilling the latter condition as it could only delete pairs that exactly matched the supplied key.

I then attempted to create a Redis clone that also support regular expressions (which works well for a while), but as the predicates became more complex, 
it became apparent that a dedicated solution for this very niche problem was needed. 
The result is Astra, a caching database that supports the insertion, aggregation, and deletion of structured data.

## Running Astra.Server

To use Astra in TCP server mode, clone this repo and build `Astra.Server` 

Run the server using the following command

```shell
ASTRA_CONFIG_PATH=/path/to/your/config.json ./Astra.Server
```

Replace the path after `ASTRA_CONFIG_PATH` with an appropriate path to the application configs.
Here is an example configuration file:

```json
{
    "logLevel": "debug",
    "authenticationMethod": "no_authentication",
    "timeout": 100000,
    "schema": {
        "columns": [
            {
                "name": "col1",
                "dataType" : "dword",
                "indexed": true
            },
            {
                "name": "col2",
                "dataType" : "string",
                "indexed": true
            },
            {
                "name": "col3",
                "dataType" : "string",
                "indexed": true
            }
        ]
    }
}
```

### Configurations

- `logLevel`: The desired level of detail for log messages. Options include `trace`, `debug`, `info`, `warn`, `error` and `critical`, 
indicating the verbosity of log output.
- `timeout`: The maximum time, in milliseconds, that the server will wait for a client to respond during connection setup. 
If a client doesn't respond within this time, the connection attempt will time out.
- `schema`: The primary definition of the database schema, specifying the structure and characteristics of the stored data.
This includes details like column names, data types, and indexing settings.

#### Authentication method:

Astra.Server supports 3 authentication methods:

- `no_authentication`: No authentication
- `password`: Password authentication using SHA-256
- `public_key`: Public-private key authentication using RSA-1024

#### Data types:

There are currently 3 supported data types, with more on the way:

- `dword`: signed 32-bit integer
- `string`: Variable-length string with a maximum length of `2147483647` characters.
- `bytes`: Variable-length byte array with a maximum length of `2147483647` bytes.

## Roadmap

- [x] Embeddable engine
- [x] TCP server
- [x] Working client
- [x] Client handshaking and authentication (SHA256, RSA)
- [x] Aggregation
- [x] Insertion/Bulk insertion
- [x] Conditional deletion
- [x] ORM (partially finished)
- [ ] .NET 6 supports for `Astra.Client`
- [ ] More data types
- [ ] Ranged query
- [ ] Regex supports
- [ ] Multiple tables
- [ ] Strict schema check
- [ ] More feature?
- [ ] Fix more bugs?
- [ ] Run faster?

## Benchmark results (January 28th, 2024)

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

// * Legends *
  BulkInsertAmount : Value of the 'BulkInsertAmount' parameter
  AggregatedRows   : Value of the 'AggregatedRows' parameter
  Mean             : Arithmetic mean of all measurements
  Error            : Half of 99.9% confidence interval
  StdDev           : Standard deviation of all measurements
  Median           : Value separating the higher half of all measurements (50th percentile)
  1 us             : 1 Microsecond (0.000001 sec)
  1 ms             : 1 Millisecond (0.001 sec)
```

- LocalBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |         Mean |      Error |       Median |       StdDev |
|:-----------------------|-----------------:|-------------:|-----------:|-------------:|-------------:|
| BulkInsertionBenchmark |               10 |     41.82 us |   1.597 us |     40.33 us |     4.558 us |
| BulkInsertionBenchmark |              100 |    392.22 us |  36.804 us |    337.27 us |   106.776 us |
| BulkInsertionBenchmark |             1000 |  2,904.04 us | 511.442 us |  2,711.17 us | 1,507.998 us |
| BulkInsertionBenchmark |            10000 | 12,181.79 us | 242.028 us | 12,127.98 us |   654.336 us |
  
- NetworkBulkInsertionBenchmark:

| Method                 | BulkInsertAmount |      Mean |     Error |     StdDev |    Median |
|:-----------------------|-----------------:|----------:|----------:|-----------:|----------:|
| BulkInsertionBenchmark |               10 | 87.910 ms | 1.6549 ms |  1.4670 ms | 88.577 ms |
| BulkInsertionBenchmark |              100 | 86.504 ms | 1.6421 ms |  1.6864 ms | 87.688 ms |
| BulkInsertionBenchmark |             1000 | 22.668 ms | 7.5089 ms | 22.1402 ms |  3.340 ms |
| BulkInsertionBenchmark |             2000 |  6.081 ms | 0.0550 ms |  0.1307 ms |  6.038 ms |



(benchmarkDotNet gets stuck at preparation step when bulk inserting more than 3000 rows - at least on my device)

- LocalSimpleAggregationBenchmark

| Method                                       | AggregatedRows |        Mean |     Error |      StdDev |      Median |
|:---------------------------------------------|---------------:|------------:|----------:|------------:|------------:|
| SimpleAggregationBenchmark                   |            100 |    116.2 us |   2.27 us |     3.25 us |    115.1 us |
| SimpleAggregationAndDeserializationBenchmark |            100 |    171.5 us |   3.43 us |     5.73 us |    170.3 us |
| SimpleAggregationBenchmark                   |           1000 |  1,090.9 us |  21.66 us |    29.65 us |  1,086.2 us |
| SimpleAggregationAndDeserializationBenchmark |           1000 |  1,979.0 us | 144.56 us |   426.25 us |  2,222.7 us |
| SimpleAggregationBenchmark                   |          10000 | 14,370.7 us | 285.54 us |   716.36 us | 14,021.2 us |
| SimpleAggregationAndDeserializationBenchmark |          10000 | 15,012.5 us | 368.31 us | 1,050.80 us | 14,530.8 us |

- IntegerBTreeInsertionBenchmark

| Method    | Degree | InsertionAmount | Mean        |     Error |      StdDev |
|:----------|-------:|----------------:|-------------|----------:|------------:|
| BTree     |     10 |            1000 |    642.2 us |  12.67 us |    13.56 us |
| Reference |     10 |            1000 |    702.3 us |  13.92 us |    13.68 us |
| BTree     |     10 |           10000 |  1,417.9 us |  37.96 us |    99.99 us |
| Reference |     10 |           10000 |  6,021.8 us |  84.08 us |    78.65 us |
| BTree     |     10 |          100000 | 20,309.5 us | 340.60 us |   318.60 us |
| Reference |     10 |          100000 | 28,894.1 us | 572.51 us | 1,642.64 us |
| BTree     |    100 |            1000 |    753.9 us |  16.47 us |    47.53 us |
| Reference |    100 |            1000 |    622.2 us |  13.01 us |    37.94 us |
| BTree     |    100 |           10000 |  1,518.3 us |  15.81 us |    29.31 us |
| Reference |    100 |           10000 |  5,854.6 us | 116.87 us |   185.38 us |
| BTree     |    100 |          100000 | 18,731.7 us | 163.05 us |   136.15 us |
| Reference |    100 |          100000 | 29,008.7 us | 308.67 us |   288.73 us |

- IntegerBTreePointQueryBenchmark

| Method         | Degree | InsertionAmount | RepeatCount |        Mean |     Error |    StdDev |      Median |
|:---------------|-------:|----------------:|------------:|------------:|----------:|----------:|------------:|
| BTreePoint     |     10 |           10000 |       10000 |  1,461.5 us |  28.44 us |  40.79 us |  1,452.1 us |
| ReferencePoint |     10 |           10000 |       10000 |    968.4 us |  37.17 us | 105.44 us |    981.5 us |
| BTreePoint     |     10 |           10000 |      100000 | 13,751.8 us |  70.22 us |  62.25 us | 13,737.9 us |
| ReferencePoint |     10 |           10000 |      100000 |  9,541.7 us |  84.74 us |  79.26 us |  9,555.4 us |
| BTreePoint     |     10 |          100000 |       10000 |  1,988.7 us |  39.59 us |  47.13 us |  1,991.6 us |
| ReferencePoint |     10 |          100000 |       10000 |  1,494.3 us |  24.47 us |  22.89 us |  1,490.0 us |
| BTreePoint     |     10 |          100000 |      100000 | 19,138.2 us | 248.55 us | 232.49 us | 19,125.2 us |
| ReferencePoint |     10 |          100000 |      100000 | 15,930.2 us | 183.75 us | 171.88 us | 15,902.1 us |
| BTreePoint     |    100 |           10000 |       10000 |  1,473.9 us | 161.81 us | 448.37 us |  1,154.0 us |
| ReferencePoint |    100 |           10000 |       10000 |    984.8 us |  18.30 us |  40.92 us |    990.8 us |
| BTreePoint     |    100 |           10000 |      100000 | 10,619.1 us | 209.23 us | 325.75 us | 10,679.4 us |
| ReferencePoint |    100 |           10000 |      100000 |  9,686.0 us | 105.39 us |  98.58 us |  9,636.0 us |
| BTreePoint     |    100 |          100000 |       10000 |  1,517.4 us |  29.44 us |  52.33 us |  1,521.4 us |
| ReferencePoint |    100 |          100000 |       10000 |  1,554.9 us |  52.18 us | 153.04 us |  1,496.6 us |
| BTreePoint     |    100 |          100000 |      100000 | 14,335.0 us | 235.40 us | 208.68 us | 14,322.7 us |
| ReferencePoint |    100 |          100000 |      100000 | 13,807.3 us | 198.33 us | 185.51 us | 13,846.8 us |
| BTreePoint     |   1000 |           10000 |       10000 |    968.5 us |  10.06 us |  25.06 us |    967.8 us |
| ReferencePoint |   1000 |           10000 |       10000 |    982.5 us |  13.99 us |  12.40 us |    978.8 us |
| BTreePoint     |   1000 |           10000 |      100000 |  9,288.1 us | 107.92 us |  90.12 us |  9,287.2 us |
| ReferencePoint |   1000 |           10000 |      100000 |  9,552.3 us |  82.90 us |  69.23 us |  9,551.8 us |
| BTreePoint     |   1000 |          100000 |       10000 |  1,367.5 us |  27.00 us |  58.13 us |  1,361.4 us |
| ReferencePoint |   1000 |          100000 |       10000 |  1,505.0 us |  29.78 us |  69.62 us |  1,495.0 us |
| BTreePoint     |   1000 |          100000 |      100000 | 12,629.6 us | 248.26 us | 254.94 us | 12,661.1 us |
| ReferencePoint |   1000 |          100000 |      100000 | 13,769.3 us | 199.88 us | 177.19 us | 13,778.1 us |

- IntegerBTreeRangeQueryBenchmark

| Method          | Degree | InsertionAmount | FetchAmount | RepeatCount |       Mean |     Error |    StdDev |
|:----------------|-------:|----------------:|------------:|------------:|-----------:|----------:|----------:|
| BTreeRangeQuery |     10 |           10000 |        1000 |         100 |   2.250 ms | 0.0432 ms | 0.0547 ms |
| BTreeRangeQuery |     10 |           10000 |        1000 |        1000 |  21.271 ms | 0.2989 ms | 0.2650 ms |
| BTreeRangeQuery |     10 |           10000 |        5000 |         100 |  11.133 ms | 0.0707 ms | 0.0590 ms |
| BTreeRangeQuery |     10 |           10000 |        5000 |        1000 | 105.453 ms | 1.1872 ms | 1.0524 ms |
| BTreeRangeQuery |     10 |           10000 |        8000 |         100 |  16.555 ms | 0.1470 ms | 0.1148 ms |
| BTreeRangeQuery |     10 |           10000 |        8000 |        1000 | 167.346 ms | 0.9126 ms | 0.8090 ms |
| BTreeRangeQuery |     10 |          100000 |        1000 |         100 |   2.174 ms | 0.0429 ms | 0.0527 ms |
| BTreeRangeQuery |     10 |          100000 |        1000 |        1000 |  22.130 ms | 0.4346 ms | 0.5338 ms |
| BTreeRangeQuery |     10 |          100000 |        5000 |         100 |  11.082 ms | 0.1614 ms | 0.1431 ms |
| BTreeRangeQuery |     10 |          100000 |        5000 |        1000 | 106.102 ms | 0.6635 ms | 0.5180 ms |
| BTreeRangeQuery |     10 |          100000 |        8000 |         100 |  17.224 ms | 0.2140 ms | 0.1897 ms |
| BTreeRangeQuery |     10 |          100000 |        8000 |        1000 | 168.442 ms | 0.7372 ms | 0.6896 ms |
| BTreeRangeQuery |    100 |           10000 |        1000 |         100 |   1.313 ms | 0.0247 ms | 0.0304 ms |
| BTreeRangeQuery |    100 |           10000 |        1000 |        1000 |  13.371 ms | 0.1854 ms | 0.1735 ms |
| BTreeRangeQuery |    100 |           10000 |        5000 |         100 |   6.396 ms | 0.1177 ms | 0.1101 ms |
| BTreeRangeQuery |    100 |           10000 |        5000 |        1000 |  67.979 ms | 0.7021 ms | 0.6223 ms |
| BTreeRangeQuery |    100 |           10000 |        8000 |         100 |  10.710 ms | 0.0950 ms | 0.0888 ms |
| BTreeRangeQuery |    100 |           10000 |        8000 |        1000 | 103.683 ms | 0.7735 ms | 0.7236 ms |
| BTreeRangeQuery |    100 |          100000 |        1000 |         100 |   1.361 ms | 0.0261 ms | 0.0231 ms |
| BTreeRangeQuery |    100 |          100000 |        1000 |        1000 |  13.564 ms | 0.2070 ms | 0.1936 ms |
| BTreeRangeQuery |    100 |          100000 |        5000 |         100 |   6.538 ms | 0.0981 ms | 0.0917 ms |
| BTreeRangeQuery |    100 |          100000 |        5000 |        1000 |  68.328 ms | 0.4017 ms | 0.3757 ms |
| BTreeRangeQuery |    100 |          100000 |        8000 |         100 |   9.829 ms | 0.1069 ms | 0.1000 ms |
| BTreeRangeQuery |    100 |          100000 |        8000 |        1000 | 104.960 ms | 0.5392 ms | 0.5044 ms |
| BTreeRangeQuery |   1000 |           10000 |        1000 |         100 |   1.194 ms | 0.0147 ms | 0.0137 ms |
| BTreeRangeQuery |   1000 |           10000 |        1000 |        1000 |  12.401 ms | 0.1305 ms | 0.1157 ms |
| BTreeRangeQuery |   1000 |           10000 |        5000 |         100 |   5.923 ms | 0.0613 ms | 0.0574 ms |
| BTreeRangeQuery |   1000 |           10000 |        5000 |        1000 |  58.569 ms | 0.2122 ms | 0.1985 ms |
| BTreeRangeQuery |   1000 |           10000 |        8000 |         100 |   9.265 ms | 0.0910 ms | 0.0851 ms |
| BTreeRangeQuery |   1000 |           10000 |        8000 |        1000 |  94.162 ms | 0.3023 ms | 0.2679 ms |
| BTreeRangeQuery |   1000 |          100000 |        1000 |         100 |   1.236 ms | 0.0174 ms | 0.0163 ms |
| BTreeRangeQuery |   1000 |          100000 |        1000 |        1000 |  12.256 ms | 0.0899 ms | 0.0841 ms |
| BTreeRangeQuery |   1000 |          100000 |        5000 |         100 |   6.001 ms | 0.0455 ms | 0.0425 ms |
| BTreeRangeQuery |   1000 |          100000 |        5000 |        1000 |  60.190 ms | 0.1525 ms | 0.1352 ms |
| BTreeRangeQuery |   1000 |          100000 |        8000 |         100 |   9.168 ms | 0.0743 ms | 0.0695 ms |
| BTreeRangeQuery |   1000 |          100000 |        8000 |        1000 |  94.548 ms | 0.2960 ms | 0.2472 ms |


## License

See LICENSE.txt


## Example

See `Astra.Example`
