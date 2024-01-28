using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public static class BTreeMap
{
    public const int MinDegree = 3;
}

public sealed partial class BTreeMap<TKey, TValue> : IRangeDictionary<TKey, TValue>, IReadOnlyRangeDictionary<TKey, TValue>
    where TKey : INumber<TKey>
{
    private enum InsertionResult
    {
        NoSizeChange,
        SizeChanged,
        NodeSplit,
    }
    
    private enum RemovalResult
    {
        NoSizeChange,
        SizeChanged,
        NodesMerged,
        Empty,
        ReturningInternal,
    }
    
    private readonly struct InsertionResultPayload(InsertionResult result, INode? submission)
    {
        public InsertionResult Result => result;
        public INode Node => submission!;
    }

    private readonly struct RemovalResultPayload(RemovalResult result, TValue? value)
    {
        public RemovalResult Result => result;
        public TValue Value => value!;
    }

    private interface INode : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public InternalNode? Parent { get; set; }
        public bool IsInternal { get; }
        public bool IsLeaf { get; }
        public bool IsOverweight { get; }
        public bool IsFull { get; }
        public int KeyCount { get; }
        public int Degree { get; }
        public TKey PrimaryKey { get; }
        public TKey AutoFirstKey { get; }
        public int AutoKeyCount { get; }
        public IEnumerable<TKey> Keys { get; }
        public IEnumerable<TValue> Values { get; }

        public InsertionResultPayload Insert(TKey key, TValue value);
        public RemovalResultPayload Remove(TKey key);
        public bool CanMergeWith<T>(T node) where T : INode;
        public void LeftMergeWith(INode node);
        public void RightMergeWith(INode node);
        public bool Contains(TKey key);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
        public IEnumerable<KeyValuePair<TKey, TValue>> Collect(TKey leftBound, TKey rightBound, CollectionMode mode);
    }

    private partial class InternalNode : INode;
    private partial class LeafNode : INode;
}