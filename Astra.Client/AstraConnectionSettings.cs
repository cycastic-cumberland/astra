using Astra.Engine;
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

    public Task<SimpleAstraClient> CreateSimpleClient() => SimpleAstraClient.Create(this);
}