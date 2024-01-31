namespace Astra.Collections;

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
}