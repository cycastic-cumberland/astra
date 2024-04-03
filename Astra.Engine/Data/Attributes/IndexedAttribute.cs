using Astra.Common.Data;

namespace Astra.Engine.Data.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexedAttribute : Attribute
{
    public IndexerType Indexer { get; set; } = IndexerType.None;
}
