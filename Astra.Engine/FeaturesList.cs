using System.Collections;
using System.Runtime.CompilerServices;
using Astra.Common.Data;

namespace Astra.Engine;

public readonly struct FeaturesList(uint[] array) : IReadOnlyList<uint>
{
    public ListEnumerator<uint, FeaturesList> GetEnumerator()
    {
        return new(this);
    }
    
    IEnumerator<uint> IEnumerable<uint>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => array.Length;

    public uint this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => array[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FeaturesList(uint[] array) => new(array);
    
    public static FeaturesList None
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Array.Empty<uint>());
    }
}