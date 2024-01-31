using System.Diagnostics;
using System.Numerics;

namespace Astra.Collections;

public sealed class DictionaryDebugView<TKey, TValue> where TKey : INumber<TKey>
{
    private readonly IReadOnlyDictionary<TKey, TValue> _dict;

    public DictionaryDebugView(IReadOnlyDictionary<TKey, TValue> dict)
    {
        ArgumentNullException.ThrowIfNull(dict, nameof(dict));
        _dict = dict;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items => _dict.ToArray();
}