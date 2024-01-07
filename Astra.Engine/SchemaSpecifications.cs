using Newtonsoft.Json;

namespace Astra.Engine;

public struct ColumnSchemaSpecifications
{
    public string Name { get; set; }
    public uint DataType { get; set; }
    public bool Indexed { get; set; }
    public bool? ShouldBeHashed { get; set; }
}

public struct SchemaSpecifications
{
    public ColumnSchemaSpecifications[] Columns { get; set; }
}

public struct RepresentableColumnSchemaSpecifications
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("dataType")]
    public string DataType { get; set; }
    [JsonProperty("indexed")]
    public bool Indexed { get; set; }
    [JsonProperty("shouldBeHashed")]
    public bool? ShouldBeHashed { get; set; }

    public ColumnSchemaSpecifications ToInternal()
    {
        var dataType = DataType switch
        {
            "DWord" => Astra.Engine.DataType.DWordMask,
            "dword" => Astra.Engine.DataType.DWordMask,
            "DWORD" => Astra.Engine.DataType.DWordMask,
            "String" => Astra.Engine.DataType.StringMask,
            "string" => Astra.Engine.DataType.StringMask,
            "STRING" => Astra.Engine.DataType.StringMask,
            "Bytes" => Astra.Engine.DataType.BytesMask,
            "bytes" => Astra.Engine.DataType.BytesMask,
            "BYTES" => Astra.Engine.DataType.BytesMask,
            _ => throw new NotSupportedException($"Data type not supported: {DataType}")
        };
        return new()
        {
            Name = Name,
            DataType = dataType,
            Indexed = Indexed,
            ShouldBeHashed = ShouldBeHashed
        };
    }
}

public struct RepresentableSchemaSpecifications
{
    [JsonProperty("columns")]
    public RepresentableColumnSchemaSpecifications[] Columns { get; set; }

    public SchemaSpecifications ToInternal()
    {
        return new()
        {
            Columns = Columns.Select(o => o.ToInternal()).ToArray()
        };
    }
}
