using System.Collections;

namespace Astra.Collections.RangeDictionaries.BTree;

public sealed partial class BTreeMap<TKey, TValue>
{
    // Supports depth first traversal for all nodes and no node
    public struct NodeEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private LocalStack<(INode node, int index)> _stack;

        public NodeEnumerator()
        {
            _stack = new(0);
        }
        
        internal NodeEnumerator(INode node, int depth)
        {
            _stack = new(depth);
            _stack.Push((node, -1));
            // _genesis = node;
        }

        public static NodeEnumerator Empty => new();

        public void Dispose()
        {
            _stack.Dispose();
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (!_stack.TryPop(out var tuple)) return false;
                var (node, index) = tuple;
                index++;
                if (index >= node.KeyCount)
                {
                    continue;
                }
                if (node is InternalNode internalNode)
                {
                    _stack.Push((node, index));
                    _stack.Push((internalNode.Children[index], -1));
                    continue;
                }
                _stack.Push((node, index)); // is leaf node
                return true;
            }
        }

        public void Reset()
        {
            // _stack.Clear();
            // _stack.Push((_genesis, -1));
            throw new NotSupportedException();
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                var (node, index) = _stack.Peek();
                return ((LeafNode)node).Pairs[index];
            }
        }

        object IEnumerator.Current => Current;
    }

}