using Newtonsoft.Json;

namespace Astra.Common.Data;

public struct ColumnSchemaSpecifications
{
    public string Name { get; set; }
    public uint DataType { get; set; }
    public IndexerData Indexer { get; set; }
    public bool? ShouldBeHashed { get; set; }
}

public struct RegistrySchemaSpecifications
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
    [JsonProperty("indexer")]
    public string Indexer { get; set; }
    [JsonProperty("shouldBeHashed")]
    public bool? ShouldBeHashed { get; set; }

    private IndexerData SelectOtherIndexer()
    {
        const string prefix = "dyn://";
        if (!Indexer.StartsWith(prefix)) goto notSupported;
        var offset = prefix.Length;
        var assemblyPathBuffer = Indexer.Length - offset < 64
            ? stackalloc char[Indexer.Length - offset]
            : new char[Indexer.Length - offset];

        int i;
        for (i = 0; i < Indexer.Length; i++)
        {
            var absolute = offset + i;
            var c = Indexer[absolute];
            if (c == ':')
                goto finalize;
            assemblyPathBuffer[i] = c;
        }

        goto notSupported;
        finalize: 
        if (offset + i >= Indexer.Length - 2) goto notSupported;
        i++;
        var assemblyPath = new string(assemblyPathBuffer[..i]);
        var className = Indexer[(offset + i)..];
        return new(IndexerType.Dynamic, new(assemblyPath, className));
        notSupported:
        throw new NotSupportedException($"Indexer not supported: {Indexer}");
    }
    
    private IndexerData SelectIndexer()
    {
        return Indexer switch
        {
            "generic" => IndexerType.Generic,
            "GENERIC" => IndexerType.Generic,
            "btree" => IndexerType.BTree,
            "BTREE" => IndexerType.BTree,
            "fuzzy" => IndexerType.Fuzzy,
            "FUZZY" => IndexerType.Fuzzy,
            "none" => IndexerType.None,
            "NONE" => IndexerType.None,
            "" => IndexerType.None,
            null => IndexerType.None,
            _ => SelectOtherIndexer()
        };
    }
    
    public ColumnSchemaSpecifications ToInternal()
    {
        var dataType = DataType switch
        {
            "DWord" => Data.DataType.DWordMask,
            "dword" => Data.DataType.DWordMask,
            "DWORD" => Data.DataType.DWordMask,
            "QWord" => Data.DataType.QWordMask,
            "qword" => Data.DataType.QWordMask,
            "QWORD" => Data.DataType.QWordMask,
            "Single" => Data.DataType.SingleMask,
            "single" => Data.DataType.SingleMask,
            "SINGLE" => Data.DataType.SingleMask,
            "Double" => Data.DataType.DoubleMask,
            "double" => Data.DataType.DoubleMask,
            "DOUBLE" => Data.DataType.DoubleMask,
            "String" => Data.DataType.StringMask,
            "string" => Data.DataType.StringMask,
            "STRING" => Data.DataType.StringMask,
            "Bytes" => Data.DataType.BytesMask,
            "bytes" => Data.DataType.BytesMask,
            "BYTES" => Data.DataType.BytesMask,
            "decimal" => Data.DataType.DecimalMask,
            "DECIMAL" => Data.DataType.DecimalMask,
            _ => throw new NotSupportedException($"Data type not supported: {DataType}")
        };
        
        return new()
        {
            Name = Name,
            DataType = dataType,
            Indexer = SelectIndexer(),
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

    public RegistrySchemaSpecifications ToInternal()
    {
        return new()
        {
            Columns = Columns.Select(o => o.ToInternal()).ToArray(),
            BinaryTreeDegree = BinaryTreeDegree
        };
    }
}
