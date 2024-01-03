using Newtonsoft.Json;

namespace Astra.Engine;

public struct ColumnSchemaSpecifications
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("dataType")]
    public uint DataType { get; set; }
    [JsonProperty("indexed")]
    public bool Indexed { get; set; }
    [JsonProperty("shouldBeHashed")]
    public bool? ShouldBeHashed { get; set; }
}

public struct SchemaSpecifications
{
    [JsonProperty("columns")]
    public ColumnSchemaSpecifications[] Columns { get; set; }
}