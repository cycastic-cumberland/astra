namespace Astra.Common;

public enum IndexerType
{
    None = 0,
    Generic = 2,
    BTree = 4,
    Fuzzy = 8,
    Rammstein = 16, // Reserved
    Dynamic = 32,
}

public readonly struct IndexerData
{
    public readonly struct DynamicInvocationInfo
    {
        public readonly string AssemblyPath;
        public readonly string IndexerClass;

        public DynamicInvocationInfo(string assembly, string indexer)
        {
            AssemblyPath = assembly;
            IndexerClass = indexer;
        }
    }
    public readonly IndexerType Type;
    public readonly DynamicInvocationInfo? Dynamic;

    public IndexerData(IndexerType type, DynamicInvocationInfo? info)
    {
        Type = type;
        Dynamic = info;
    }

    public static implicit operator IndexerData(IndexerType type) => new(type, null);
}
