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

## Benchmark results (January 6th, 2024)

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

(benchmarkDotNet gets stuck at preparation step when bulk inserting more than 3000 rows - at least on my device)

## License

See LICENSE.txt


## Example

See `Astra.Example`
