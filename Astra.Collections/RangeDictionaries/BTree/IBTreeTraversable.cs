using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public interface IBTreeTraversable<in TKey, in TValue> where TKey : INumber<TKey>
{
    public void Start(long keyCount, int degree);
    public void EnterInternal(TKey primaryKey);
    public void ExitInternal(TKey primaryKey);
    public void EnterLeaf(TKey primaryKey);
    public void ExitLeaf(TKey primaryKey);
    public void Fetch(TKey key, TValue value);
    public void Finish();
}