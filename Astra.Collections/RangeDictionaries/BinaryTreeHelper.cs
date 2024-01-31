using System.Numerics;

namespace Astra.Collections.RangeDictionaries;

internal static class BinaryTreeHelper
{
    public static int BinarySearch<TKey, TValue>(this Span<KeyValuePair<TKey, TValue>> span, TKey target)
        where TKey : INumber<TKey>
    {
        var left = 0;
        var right = span.Length - 1;
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = span[mid].Key.CompareTo(target);
            switch (comparison)
            {
                case 0: // Equal
                    return mid;
                case -1: // mid < target
                    left = mid + 1;
                    break;
                case 1: // mid > target
                    right = mid - 1;
                    break;
            }
        }

        return -1;
    }

    public static (int index, bool isExact) NearestBinarySearch<TKey, TValue>(this Span<KeyValuePair<TKey, TValue>> span, TKey target)
        where TKey : INumber<TKey>
    {
        var left = 0;
        var ceil = span.Length - 1;
        var right = ceil;
        var result = -1;
        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = span[mid].Key.CompareTo(target);
            switch (comparison)
            {
                case 0: // Equal
                    return (mid, true);
                case -1: // mid < target
                    left = mid + 1;
                    break;
                case 1: // mid > target
                    right = mid - 1;
                    result = mid;
                    break;
            }
        }

        return (result, false);
    }
}