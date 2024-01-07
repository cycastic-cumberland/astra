using Newtonsoft.Json;

namespace Astra.Engine;

public struct AstraLaunchSettings
{
    [JsonProperty("schema")]
    public SchemaSpecifications Schema { get; set; }
    [JsonProperty("logLevel")]
    public string? LogLevel { get; set; }
    [JsonProperty("timeout")]
    public int Timeout { get; set; }
    [JsonProperty("authenticationMethod")]
    public uint AuthenticationMethod { get; set; }
    [JsonProperty("hashedPasswordPath")]
    public string? HashedPasswordPath { get; set; }
    [JsonProperty("publicKeyPath")]
    public string? PublicKeyPath { get; set; }
}