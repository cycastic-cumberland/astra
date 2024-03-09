using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Types;

public readonly struct TypeHandlingResult
{
    public DataType Type { get; init; }
    public string TypeName { get; init; }
    public IColumnResolver Resolver { get; init; }
    public IIndexer? Indexer { get; init; }
    public int NewOffset { get; init; }
    public bool IsHashed { get; init; }
}

public interface ITypeHandler
{
    public uint FeaturingType { get; }
    public TypeHandlingResult Process(ColumnSchemaSpecifications column, RegistrySchemaSpecifications registry, int index, int offset);
}