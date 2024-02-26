using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

file class IndexerComparator : IEqualityComparer<IIndexer>
{
    public static readonly IndexerComparator Default = new();
    public bool Equals(IIndexer? x, IIndexer? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(IIndexer obj) => obj.GetHashCode();
}

public class CompositeIndexer<T, TColumnResolver, TStreamResolver>
    where T : notnull
    where TColumnResolver : IColumnResolver<T>
    where TStreamResolver : IStreamResolver<T>
{
    
}