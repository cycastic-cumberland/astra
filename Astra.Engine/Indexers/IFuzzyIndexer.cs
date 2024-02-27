using Astra.Engine.Data;

namespace Astra.Engine.Indexers;

public interface IFuzzyIndexer<TUnit, TSequence> 
    where TUnit : IEquatable<TUnit>
    where TSequence : IReadOnlyList<TUnit>
{
    public interface IFuzzyIndexerHandler : IDisposable
    {
        public IEnumerable<(ImmutableDataRow row, int matchLength)> FuzzySearch(TSequence sequence, int minLength);
        public IEnumerable<ImmutableDataRow> ReducedFuzzySearch(TSequence sequence, int minLength);
    }
    
    public IFuzzyIndexerHandler Read();
}

public interface IFuzzyIndexer<TUnit, TSequence, out TRead>
    where TUnit : IEquatable<TUnit>
    where TSequence : IReadOnlyList<TUnit>
    where TRead : IFuzzyIndexer<TUnit, TSequence>.IFuzzyIndexerHandler
{
    public TRead Read();
}
