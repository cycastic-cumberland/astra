using Newtonsoft.Json;

namespace Astra.Engine;

public struct AstraLaunchSettings
{
    public SchemaSpecifications Schema { get; set; }
    public string? LogLevel { get; set; }
    public int? Port { get; set; }
    public int Timeout { get; set; }
    public uint AuthenticationMethod { get; set; }
    public string? HashedPasswordPath { get; set; }
    public string? PublicKeyPath { get; set; }
}

public struct RepresentableAstraLaunchSettings
{
    [JsonProperty("schema")]
    public RepresentableSchemaSpecifications Schema { get; set; }
    [JsonProperty("logLevel")]
    public string? LogLevel { get; set; }
    [JsonProperty("port")]
    public int? Port { get; set; }
    [JsonProperty("timeout")]
    public int Timeout { get; set; }
    [JsonProperty("authenticationMethod")]
    public string AuthenticationMethod { get; set; }
    [JsonProperty("hashedPasswordPath")]
    public string? HashedPasswordPath { get; set; }
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
            "public_key" => CommunicationProtocol.PubKeyAuthentication,
            "PUBLIC_KEY" => CommunicationProtocol.PubKeyAuthentication,
            _ => throw new NotSupportedException($"Authentication method not supported: {AuthenticationMethod}")
        };
        return new()
        {
            Schema = Schema.ToInternal(),
            LogLevel = LogLevel,
            Timeout = Timeout,
            AuthenticationMethod = method,
            HashedPasswordPath = HashedPasswordPath,
            PublicKeyPath = PublicKeyPath
        };
    }
}
