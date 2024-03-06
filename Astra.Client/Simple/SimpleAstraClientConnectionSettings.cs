using Astra.Common;
using Newtonsoft.Json;

namespace Astra.Client.Simple;

public struct SimpleAstraClientConnectionSettings
{
    [JsonProperty("address")]
    public string Address { get; set; }
    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("timeout")]
    public int Timeout { get; set; }
    [JsonProperty("password")]
    public string? Password { get; set; }
    [JsonProperty("privateKey")]
    public string? PrivateKey { get; set; }
}