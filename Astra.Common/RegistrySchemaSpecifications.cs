using Newtonsoft.Json;

namespace Astra.Common;

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
