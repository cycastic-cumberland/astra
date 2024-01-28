namespace Astra.Collections.RangeDictionaries;

internal static class ArrayHelper
{
    public static void ShiftElements<T>(this T[] array, int fromBound, int toBound, int unitCount)
    {
        // Perform the shift
        for (var i = toBound; i >= fromBound; i--)
        {
            array[i + unitCount] = array[i];
#if DEBUG
            array[i] = default!;
#endif
        }
    }
    
    public static void DoubleShiftElements<T1, T2>(this (T1[] left, T2[] right) tuple, int fromBound, int toBound, int unitCount)
    {
        // Perform the shift
        for (var i = toBound; i >= fromBound; i--)
        {
            tuple.left[i + unitCount] = tuple.left[i];
            tuple.right[i + unitCount] = tuple.right[i];
#if DEBUG
            tuple.left[i] = default!;
            tuple.right[i] = default!;
#endif
        }
    }
}