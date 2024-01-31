using System.Diagnostics;
using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public sealed partial class BTreeMap<TKey, TValue> where TKey : INumber<TKey>
{
    private partial class InternalNodeDebugView(InternalNode node)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Span<INode> Children => node.Children;
    }

    private partial class LeafNodeDebugView(LeafNode node)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Span<KeyValuePair<TKey, TValue>> Pairs => node.Pairs;
    }
}