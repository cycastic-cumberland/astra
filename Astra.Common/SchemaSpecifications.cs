using Newtonsoft.Json;

namespace Astra.Common;

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
    public int BinaryTreeDegree { get; set; }
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
            "DWord" => Astra.Common.DataType.DWordMask,
            "dword" => Astra.Common.DataType.DWordMask,
            "DWORD" => Astra.Common.DataType.DWordMask,
            "QWord" => Astra.Common.DataType.QWordMask,
            "qword" => Astra.Common.DataType.QWordMask,
            "QWORD" => Astra.Common.DataType.QWordMask,
            "Single" => Astra.Common.DataType.SingleMask,
            "single" => Astra.Common.DataType.SingleMask,
            "SINGLE" => Astra.Common.DataType.SingleMask,
            "Double" => Astra.Common.DataType.DoubleMask,
            "double" => Astra.Common.DataType.DoubleMask,
            "DOUBLE" => Astra.Common.DataType.DoubleMask,
            "String" => Astra.Common.DataType.StringMask,
            "string" => Astra.Common.DataType.StringMask,
            "STRING" => Astra.Common.DataType.StringMask,
            "Bytes" => Astra.Common.DataType.BytesMask,
            "bytes" => Astra.Common.DataType.BytesMask,
            "BYTES" => Astra.Common.DataType.BytesMask,
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
    [JsonProperty("binaryTreeDegree")]
    public int BinaryTreeDegree { get; set; }

    public SchemaSpecifications ToInternal()
    {
        return new()
        {
            Columns = Columns.Select(o => o.ToInternal()).ToArray(),
            BinaryTreeDegree = BinaryTreeDegree
        };
    }
}
