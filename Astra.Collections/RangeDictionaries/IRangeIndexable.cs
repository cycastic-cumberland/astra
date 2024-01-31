using System.Numerics;

namespace Astra.Collections.RangeDictionaries;

public interface IRangeIndexable<TKey, TValue>
    where TKey : INumber<TKey>
{
    public IEnumerable<KeyValuePair<TKey, TValue>> CollectBetween(TKey fromValue, TKey toValue, bool includeFrom = true, bool includeTo = true);
    public IEnumerable<KeyValuePair<TKey, TValue>> CollectExclude(TKey fromValue, TKey toValue, bool includeFrom = true, bool includeTo = true);
    public IEnumerable<KeyValuePair<TKey, TValue>> CollectFrom(TKey fromValue, bool includeFrom = true);
    public IEnumerable<KeyValuePair<TKey, TValue>> CollectTo(TKey toValue, bool includeTo = true);
}

public interface IReadOnlyRangeDictionary<TKey, TValue>
    : IReadOnlyDictionary<TKey, TValue>, IRangeIndexable<TKey, TValue>
    where TKey : INumber<TKey>;

public interface IRangeDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>, IRangeIndexable<TKey, TValue>
    where TKey : INumber<TKey>;
