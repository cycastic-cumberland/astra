using System.Collections;
using Astra.Collections;
using Astra.Common;

namespace Astra.Engine.Indexers;

public readonly struct ComparableBytesMemory : IEquatable<ComparableBytesMemory>, IReadOnlyList<byte>
{
    private readonly ReadOnlyMemory<byte> _bytes;
    public ReadOnlyMemory<byte> Memory => _bytes;
    
    public ComparableBytesMemory(ReadOnlyMemory<byte> bytes)
    {
        _bytes = bytes;
    }
    
    public bool Equals(ComparableBytesMemory other)
    {
        return BytesComparisonHelper.Equals(_bytes, other._bytes);
    }

    public ListEnumerator<byte, ComparableBytesMemory> GetEnumerator() =>
        this.GetListEnumerator<byte, ComparableBytesMemory>();
    
    IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        return obj is ComparableBytesMemory other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _bytes.GetHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator ComparableBytesMemory(BytesCluster bytes) => new(bytes.ReaderMemory);
    public static implicit operator ComparableBytesMemory(ReadOnlyMemory<byte> bytes) => new(bytes);
    public static implicit operator ComparableBytesMemory(byte[] bytes) => new(bytes);


    public int Count => _bytes.Length;

    public byte this[int index] => _bytes.Span[index];

    public static bool operator ==(ComparableBytesMemory left, ComparableBytesMemory right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComparableBytesMemory left, ComparableBytesMemory right)
    {
        return !(left == right);
    }
}