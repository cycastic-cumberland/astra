using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public sealed partial class BTreeMap<TKey, TValue> where TKey : INumber<TKey>
{
    private readonly struct StackCollectionTray
    {
        public InternalNode Node { get; init; }
        public TKey FromBound { get; init; }
        public TKey ToBound { get; init; }
        public CollectionMode Mode { get; init; }
        public int Index { get; init; }
    }
    [DebuggerTypeProxy(typeof(RangeDictionaries.BTree.BTreeMap<,>.InternalNodeDebugView))]
    [DebuggerDisplay("PrimaryKey = {PrimaryKey}, KeyCount = {KeyCount}")]
    internal partial class InternalNode(int degree)
    {
        private readonly int _splitSize = degree / 2;
        private readonly INode[] _children = new INode[degree + 1];
        private TKey _primaryKey = NumericHelper.GetMax<TKey>();

        public InternalNode? Parent { get; set; }
        public bool IsInternal => true;
        public bool IsLeaf => false;
        public bool IsOverweight => ChildCount == degree + 1;
        public bool IsFull => ChildCount >= degree;
        public int ChildCount { get; private set; }
        public int KeyCount => ChildCount;
        public int Degree => degree;

        public TKey PrimaryKey
        {
            get => _primaryKey;
            set
            {
                if (Parent != null && Parent.PrimaryKey.Equals(_primaryKey))
                    Parent.PrimaryKey = value;
                _primaryKey = value;
            }
        }

        public TKey AutoFirstKey => _children[0].AutoFirstKey;

        public int AutoKeyCount
        {
            get
            {
                var sum = 0;
                foreach (var child in Children)
                {
                    sum += child.AutoKeyCount;
                }

                return sum;
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                for (var i = 0; i < ChildCount; i++)
                {
                    foreach (var key in _children[i].Keys)
                    {
                        yield return key;
                    }
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                for (var i = 0; i < ChildCount; i++)
                {
                    foreach (var value in _children[i].Values)
                    {
                        yield return value;
                    }
                }
            }
        }

        public Span<INode> Children => new(_children, 0, ChildCount);

        public InternalNode(int degree, INode first, INode second) : this(degree)
        {
            Adopt(first, 0);
            Adopt(second, 1);
            ChildCount = 2;
            PrimaryKey = first.PrimaryKey;
        }

        private static int NearestBinarySearch(Span<INode> span, TKey target)
        {
            var left = 0;
            var ceil = span.Length - 1;
            var right = ceil;
            var result = -1;
            // var finalized = false;
            while (left <= right)
            {
                var mid = left + (right - left) / 2;
                var comparison = span[mid].PrimaryKey.CompareTo(target);
                switch (comparison)
                {
                    case 0: // Equal
                        return mid;
                    case -1: // mid < target
                        left = mid + 1;
                        break;
                    case 1: // mid > target
                        right = mid - 1;
                        result = mid - 1;
                        break;
                }
            }

            return result;
        }

        private INode this[int index]
        {
            get => _children[index];
            set => _children[index] = value;
        }

        private InternalNode Split()
        {
            var moveIndex = ChildCount - _splitSize;
            var newNode = new InternalNode(degree)
            {
                ChildCount = _splitSize,
                Parent = Parent,
                PrimaryKey = _children[moveIndex].PrimaryKey
            };
            for (var i = 0; i < _splitSize; i++)
            {
                newNode[i] = _children[moveIndex + i];
#if DEBUG
                _children[moveIndex + i] = null!;
#endif
            }
            
            ChildCount -= _splitSize;
            return newNode;
        }

        private void Adopt(INode child, int index)
        {
            _children[index] = child;
            child.Parent = this;
        }

        private void ManuallyInsertChild(INode child, int index)
        {
            if (child.PrimaryKey.CompareTo(_children[index].PrimaryKey) > 0) 
                index++;
            for (var i = ChildCount; i > index; i--)
            {
                _children[i] = _children[i - 1];
            }

            Adopt(child, index);
            ChildCount++;
            // It never insert at index 0
            // if (index == 0)
            //     PrimaryKey = child.PrimaryKey;
        }

        public InsertionResultPayload Insert(TKey key, TValue value)
        {
            var index = NearestBinarySearch(Children, key);
            if (index == -1)
            {
                index = key.CompareTo(_children[0].PrimaryKey) < 0 ? 0 : ChildCount - 1;
            }
            var child = _children[index];
            var ret = child.IsLeaf 
                ? ((LeafNode)child).Insert(key, value) 
                : ((InternalNode)child).Insert(key, value);
            switch (ret.Result)
            {
                case InsertionResult.NoSizeChange:
                    return ret;
                case InsertionResult.SizeChanged:
                {
                    return new(InsertionResult.SizeChanged, null);
                }
                case InsertionResult.NodeSplit:
                {
                    var split = ret.Node;
                    ManuallyInsertChild(split, index);
                    if (!IsOverweight) return new(InsertionResult.SizeChanged, null);
                    var newNode = Split();
                    return new(InsertionResult.NodeSplit, newNode);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RemoveNode(int index)
        {
            for (var i = index; i < ChildCount - 1; i++)
            {
                this[i] = this[i + 1];
            }

            ChildCount--;
        }
        
        
        public RemovalResultPayload Remove(TKey key)
        {
            var index = NearestBinarySearch(Children, key);
            if (index == -1)
            {
                if (key.CompareTo(_children[0].PrimaryKey) < 0)
                    return new(RemovalResult.NoSizeChange, default);
                index = ChildCount - 1;
            }
            if (index == -1) return new(RemovalResult.NoSizeChange, default);
            var child = _children[index];
            var ret = child.IsLeaf 
                ? ((LeafNode)child).Remove(key) 
                : ((InternalNode)child).Remove(key);
            
            switch (ret.Result)
            {
                case RemovalResult.Empty:
                {
                    if (ChildCount == 1)
                    {
#if DEBUG
                        if (index != 0)
                        {
                            throw new UnreachableException();
                        }
#endif
                        _children[0] = null!;
                        ChildCount--;
                        return new(RemovalResult.Empty, ret.Value);
                    }

                    if (index == 0)
                    {
                        PrimaryKey = _children[1].PrimaryKey;
                    }
                    RemoveNode(index);
                    return new(RemovalResult.SizeChanged, ret.Value);
                }
                case RemovalResult.NoSizeChange:
                {
                    return ret;
                }
                case RemovalResult.SizeChanged:
                {
                    if (index > 0 && _children[index - 1].CanMergeWith(child))
                    {
                        _children[index - 1].RightMergeWith(child);
                        RemoveNode(index);
                        if (ChildCount == 0) return new(RemovalResult.Empty, ret.Value);
                        return new(RemovalResult.NodesMerged, ret.Value);
                    }
                    
                    if (index < ChildCount - 1 && _children[index + 1].CanMergeWith(child))
                    {
                        _children[index + 1].LeftMergeWith(child);
                        RemoveNode(index);
                        if (ChildCount == 0) return new(RemovalResult.Empty, ret.Value);
                        return new(RemovalResult.NodesMerged, ret.Value);
                    }

                    return new(RemovalResult.SizeChanged, ret.Value);
                }
                case RemovalResult.NodesMerged:
                {
                    goto case RemovalResult.SizeChanged;
                }
                case RemovalResult.ReturningInternal:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool CanMergeWith<T>(T node) where T : INode
        {
            return node.IsInternal && ChildCount >= node.KeyCount && ChildCount + node.KeyCount <= degree;
        }

        public void LeftMergeWith(INode node)
        {
            var target = (InternalNode)node;
            var oldFirst = AutoFirstKey;
            var targetSize = target.ChildCount;
            var oldSize = ChildCount;
            var newSize = oldSize + targetSize;
            _children.ShiftElements(0, oldSize - 1, targetSize);
            for (var i = 0; i < targetSize; i++)
            {
                this[i] = target[i];
            }
            ChildCount = newSize;
            PrimaryKey = _children[0].PrimaryKey;
            if (Parent != null && Parent.PrimaryKey.Equals(oldFirst))
                Parent.PrimaryKey = PrimaryKey;
        }

        public void RightMergeWith(INode node)
        {
            var target = (InternalNode)node;
            var targetSize = target.ChildCount;
            var oldSize = ChildCount;
            for (var i = 0; i < targetSize; i++)
            {
                this[oldSize + i] = target[i];
            }

            ChildCount = oldSize + targetSize;
        }

        private static bool ContainsInternal(InternalNode genesis, TKey key)
        {
            var node = genesis;
            while (true)
            {
                var index = NearestBinarySearch(node.Children, key);
                if (index == -1)
                {
                    if (key.CompareTo(node.Children[0].PrimaryKey) < 0)
                        break;
                    index = node.ChildCount - 1;
                }
                var child = node.Children[index];
                if (child.IsLeaf)
                    return ((LeafNode)child).Contains(key);
                node = (InternalNode)child;
            }

            return false;
        }

        public bool Contains(TKey key)
        {
            return ContainsInternal(this, key);
        }

        private static bool TryGetValueInternal(InternalNode genesis, TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            var node = genesis;
            while (true)
            {
                var index = NearestBinarySearch(node.Children, key);
                if (index == -1)
                {
                    if (key.CompareTo(node.Children[0].PrimaryKey) < 0)
                        break;
                    index = node.ChildCount - 1;
                }
                var child = node.Children[index];
                if (child.IsLeaf)
                    return ((LeafNode)child).TryGetValue(key, out value);
                node = (InternalNode)child;
            }

            value = default;
            return false;
        }
        
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return TryGetValueInternal(this, key, out value);
        }

        public NodeEnumerator GetEnumerator(int depth)
        {
            return new(this, depth);
        }

        // StackCollect actually use the heap as its primary storage lol
        private static IEnumerable<KeyValuePair<TKey, TValue>> StackCollect(int depth, InternalNode node, 
            TKey leftBound, TKey rightBound, CollectionMode mode)
        {
            using var stack = new ArrayStack<StackCollectionTray>(depth);
            stack.Push(new()
            {
                Node = node,
                FromBound = leftBound,
                ToBound = rightBound,
                Mode = mode,
                Index = -1
            });
            while (stack.TryPop(out var tray))
            {
                if (tray.Index > -1)
                {
                    var index = tray.Index;
                    INode child;
                    if (index < tray.Node.ChildCount && (child = tray.Node[index]).PrimaryKey.CompareTo(tray.ToBound) <= 0)
                    {
                        stack.Push(tray with { Index = index + 1 });
                        if (child.IsLeaf)
                            foreach (var kp in ((LeafNode)child).Collect(depth, tray.FromBound, tray.ToBound, tray.Mode))
                            {
                                yield return kp;
                            }
                        else
                        {
                            stack.Push(tray with { Index = -1, Node = (InternalNode)child });
                        }
                    }
                    continue;
                }
                switch (tray.Mode)
                {
                    case CollectionMode.HalfClosedLeftInterval:
                    case CollectionMode.HalfClosedRightInterval:
                    case CollectionMode.ClosedInterval:
                    {
                        var index = NearestBinarySearch(tray.Node.Children, tray.FromBound);
                        if (index == -1)
                        {
                            index = tray.FromBound.CompareTo(tray.Node.Children[0].PrimaryKey) < 0 
                                ? 0
                                : tray.Node.ChildCount - 1;
                        }
                        // if (index == 0 && tray.Node[index].PrimaryKey.CompareTo(tray.FromBound) < 0) break;
                        stack.Push(tray with { Index = index });
                        break;
                    }
                    case CollectionMode.OpenInterval:
                    // {
                    //     break;
                    // }
                    case CollectionMode.UnboundedClosedInterval:
                    // {
                    //     break;
                    // }
                    case CollectionMode.UnboundedHalfClosedLeftInterval:
                    case CollectionMode.UnboundedHalfClosedRightInterval:
                    case CollectionMode.UnboundedOpenInterval:
                    // {
                    //     break;
                    // }
                    {
                        throw new NotSupportedException();
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Collect(int depth, TKey leftBound, TKey rightBound, CollectionMode mode)
        {
            return StackCollect(depth, this, leftBound, rightBound, mode);
        }

        public void Traverse<T>(T traversable) where T : IBTreeTraversable<TKey, TValue>
        {
            traversable.EnterInternal(PrimaryKey);
            foreach (var child in Children)
            {
                child.Traverse(traversable);
            }
            traversable.ExitInternal(PrimaryKey);
        }
    }
}