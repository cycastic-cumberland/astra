namespace Astra.Engine.v2.Indexers;

internal static class IndexerHelpers
{
    public static HashSet<T>? ToHashSetOrNull<T>(this IEnumerable<T> enumerable)
    {
        HashSet<T>? set = null;
        foreach (var value in enumerable)
        {
            set ??= new();
            set.Add(value);
        }

        return set;
    }
}