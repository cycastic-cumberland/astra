using Newtonsoft.Json;

namespace Astra.Engine;

public struct AstraLaunchSettings
{
    [JsonProperty("schema")]
    public SchemaSpecifications Schema { get; set; }
    [JsonProperty("logLevel")]
    public string? LogLevel { get; set; }
}