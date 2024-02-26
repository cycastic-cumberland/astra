using System.Diagnostics;

namespace Astra.Collections.WideDictionary;

public class DictionaryDebugView<TKey, TValue>(IDictionary<TKey, TValue> dictionary) where TKey : notnull
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Pairs => dictionary.ToArray();
}