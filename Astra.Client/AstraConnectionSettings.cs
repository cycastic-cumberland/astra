using Astra.Common;
using Newtonsoft.Json;

namespace Astra.Client;

public struct AstraConnectionSettings
{
    [JsonProperty("address")]
    public string Address { get; set; }
    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("schema")]
    public SchemaSpecifications Schema { get; set; }
    [JsonProperty("timeout")]
    public int Timeout { get; set; }
    [JsonProperty("password")]
    public string? Password { get; set; }
    [JsonProperty("privateKey")]
    public string? PrivateKey { get; set; }
}