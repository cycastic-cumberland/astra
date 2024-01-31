using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public class MaxDepthExplorer<TKey, TValue> : IBTreeTraversable<TKey, TValue> where TKey : INumber<TKey>
{
    public int MaxDepth { get; private set; }
    private int _depth;
    public void Start(long keyCount, int degree)
    {
        _depth = 0;
        MaxDepth = 0;
    }

    private void RaiseDepth()
    {
        MaxDepth = Math.Max(MaxDepth, ++_depth);
    }

    private void LowerDepth()
    {
        _depth--;
    }

    public void EnterInternal(TKey primaryKey)
    {
        RaiseDepth();
    }

    public void ExitInternal(TKey primaryKey)
    {
        LowerDepth();
    }

    public void EnterLeaf(TKey primaryKey)
    {
        RaiseDepth();
    }

    public void ExitLeaf(TKey primaryKey)
    {
        LowerDepth();
    }

    public void Fetch(TKey key, TValue value)
    {
        
    }

    public void Finish()
    {
        
    }
}