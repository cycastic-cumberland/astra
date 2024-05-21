using Astra.Common.Protocols;
using Newtonsoft.Json;

namespace Astra.Common.Data;

public struct AstraLaunchSettings
{
    public RegistrySchemaSpecifications Schema { get; set; }
    public bool UseCellBasedDataStore { get; set; }
    public CompressionOptions CompressionOption { get; set; }
    public string? LogLevel { get; set; }
    public int? Port { get; set; }
    public int Timeout { get; set; }
    public uint AuthenticationMethod { get; set; }
    public string? Password { get; set; }
    public string? PublicKeyPath { get; set; }
}

public struct RepresentableAstraLaunchSettings
{
    [JsonProperty("schema")]
    public RepresentableSchemaSpecifications Schema { get; set; }
    [JsonProperty("useCellBasedDataStore")]
    public bool? UseCellBasedDataStore { get; set; }
    [JsonProperty("compressionOption")]
    public string? CompressionOption { get; set; }
    [JsonProperty("logLevel")]
    public string? LogLevel { get; set; }
    [JsonProperty("port")]
    public int? Port { get; set; }
    [JsonProperty("timeout")]
    public int Timeout { get; set; }
    [JsonProperty("authenticationMethod")]
    public string AuthenticationMethod { get; set; }
    [JsonProperty("password")]
    public string? Password { get; set; }
    [JsonProperty("publicKeyPath")]
    public string? PublicKeyPath { get; set; }

    public AstraLaunchSettings ToInternal()
    {
        var method = AuthenticationMethod switch
        {
            "no_authentication" => CommunicationProtocol.NoAuthentication,
            "NO_AUTHENTICATION" => CommunicationProtocol.NoAuthentication,
            "password" => CommunicationProtocol.PasswordAuthentication,
            "PASSWORD" => CommunicationProtocol.PasswordAuthentication,
            "salted_password" => CommunicationProtocol.PasswordAuthentication,
            "SALTED_PASSWORD" => CommunicationProtocol.SaltedPasswordAuthentication,
            "public_key" => CommunicationProtocol.PubKeyAuthentication,
            "PUBLIC_KEY" => CommunicationProtocol.PubKeyAuthentication,
            _ => throw new NotSupportedException($"Authentication method not supported: {AuthenticationMethod}")
        };
        var compressionStrategy = CompressionOption switch
        {
            "gzip" => ConnectionFlags.CompressionOptions.GZip | ConnectionFlags.CompressionOptions.Optimal,
            "gzip+speed" => ConnectionFlags.CompressionOptions.GZip |  ConnectionFlags.CompressionOptions.Fastest,
            "gzip+size" => ConnectionFlags.CompressionOptions.GZip |  ConnectionFlags.CompressionOptions.SmallestSize,
            "deflate" => ConnectionFlags.CompressionOptions.Deflate | ConnectionFlags.CompressionOptions.Optimal,
            "deflate+speed" => ConnectionFlags.CompressionOptions.Deflate |  ConnectionFlags.CompressionOptions.Fastest,
            "deflate+size" => ConnectionFlags.CompressionOptions.Deflate |  ConnectionFlags.CompressionOptions.SmallestSize,
            "brotli" => ConnectionFlags.CompressionOptions.Brotli | ConnectionFlags.CompressionOptions.Optimal,
            "brotli+speed" => ConnectionFlags.CompressionOptions.Brotli |  ConnectionFlags.CompressionOptions.Fastest,
            "brotli+size" => ConnectionFlags.CompressionOptions.Brotli |  ConnectionFlags.CompressionOptions.SmallestSize,
            "zlib" => ConnectionFlags.CompressionOptions.ZLib | ConnectionFlags.CompressionOptions.Optimal,
            "zlib+speed" => ConnectionFlags.CompressionOptions.ZLib |  ConnectionFlags.CompressionOptions.Fastest,
            "zlib+size" => ConnectionFlags.CompressionOptions.ZLib |  ConnectionFlags.CompressionOptions.SmallestSize,
            "lz4" => ConnectionFlags.CompressionOptions.LZ4 | ConnectionFlags.CompressionOptions.Optimal,
            "lz4+speed" => ConnectionFlags.CompressionOptions.LZ4 |  ConnectionFlags.CompressionOptions.Fastest,
            "lz4+size" => ConnectionFlags.CompressionOptions.LZ4 |  ConnectionFlags.CompressionOptions.SmallestSize,
            _ => ConnectionFlags.CompressionOptions.None,
        };
        return new()
        {
            UseCellBasedDataStore = UseCellBasedDataStore ?? false,
            CompressionOption = (CompressionOptions)compressionStrategy,
            Schema = Schema.ToInternal(),
            Port = Port,
            LogLevel = LogLevel,
            Timeout = Timeout,
            AuthenticationMethod = method,
            Password = Password,
            PublicKeyPath = PublicKeyPath
        };
    }
}
