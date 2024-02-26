namespace Astra.Collections.Trigram;

public interface ITrigramMap<TKey, TValue> : IDictionary<TKey, TValue>
{
    public IEnumerable<KeyValuePair<TKey, (TValue value, int matchedLength)>> FuzzySearch(TKey key, int lengthFilter = -1);
}