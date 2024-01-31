using System.Numerics;

namespace Astra.Collections.RangeDictionaries.BTree;

public class ConsoleView<TKey, TValue>(int indent = 4, int initialIndent = 0) : IBTreeTraversable<TKey, TValue> where TKey : INumber<TKey>
{
    private int _currentIndent = initialIndent;
    private int _depth;
    private string Padding => new(' ', _currentIndent);
    private string _prefix = "";

    private string Pad(string str) => $"{Padding}{str}";

    public static ConsoleView<TKey, TValue> New => new();
    
    public void Start(long keyCount, int degree)
    {
        _currentIndent = initialIndent;
        _depth = 0;
        _prefix = "";
        Console.WriteLine(Pad($"BTree(keyCount={keyCount}, degree={degree})"));
    }

    public void EnterInternal(TKey primaryKey)
    {
        _currentIndent += indent;
        _depth++;
        Console.WriteLine(Pad($"- Internal(primaryKey={primaryKey}, depth={_depth})"));
    }

    public void ExitInternal(TKey primaryKey)
    {
        _currentIndent -= indent;
        _depth--;
    }

    public void EnterLeaf(TKey primaryKey)
    {
        _currentIndent += indent;
        _prefix = "";
        _depth++;
        Console.Write(Pad($"+ Leaf(primaryKey={primaryKey}, depth={_depth}):"));
    }

    public void ExitLeaf(TKey primaryKey)
    {
        _currentIndent -= indent;
        _depth--;
        Console.WriteLine();
    }

    public void Fetch(TKey key, TValue value)
    {
        Console.Write($"{_prefix} {new KeyValuePair<TKey,TValue>(key, value)}");
        _prefix = ",";
    }

    public void Finish()
    {
        
    }
}