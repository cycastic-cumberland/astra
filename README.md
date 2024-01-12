# Astra
 
Astra is a lightweight, tabular database for fast and effective caching of structured data.

## Requirements

Requires .NET 8 for both client and server (there is no good reason for this, I just don't know how to install .NET 6 on Arch Linux)

## Background

Homeworks are boring as hell so I'm making a database instead

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
- [ ] More data types
- [ ] Ranged query
- [ ] Multiple tables
- [ ] Strict schema check
- [ ] More feature?
- [ ] Fix more bugs?
- [ ] Run faster?
 
## Benchmark results (January 10th, 2024)

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



## License

See LICENSE.txt


## Example

See `Astra.Example`
