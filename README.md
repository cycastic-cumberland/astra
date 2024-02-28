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
                "indexer": "none"
            },
            {
                "name": "col2",
                "dataType" : "string",
                "indexer": "fuzzy"
            },
            {
                "name": "col3",
                "dataType" : "string",
                "indexer": "none"
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
- `qword`: signed 64-bit integer
- `single`: single precision floating point number
- `double`: double precision floating point number
- `string`: Variable-length string with a maximum length of `2147483647` characters.
- `bytes`: Variable-length byte array with a maximum length of `2147483647` bytes.

## Feature

- [x] Embeddable engine
- [x] TCP server
- [x] Working client
- [x] Client handshaking and authentication (SHA256, RSA)
- [x] Aggregation
- [x] Insertion/Bulk insertion
- [x] Conditional deletion
- [x] ORM (partially finished)
- [x] Fuzzy string search supports
- [x] Supports for qword, single and double
- [x] Ranged query
- [ ] .NET 6 supports for `Astra.Client`
- [ ] Multiple tables
- [ ] Strict schema check

## Benchmark results

See the [benchmark](/benchmarks/) folder

## License

See LICENSE.txt


## Example

See `Astra.Example`
