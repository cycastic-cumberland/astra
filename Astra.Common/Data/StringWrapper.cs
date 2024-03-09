using System.Collections;
using System.Runtime.CompilerServices;

namespace Astra.Common.Data;

public readonly struct StringWrapper(string innerString) : 
    IReadOnlyList<char>, 
    IEquatable<StringWrapper>, 
    IEquatable<string>,
    IComparable<StringWrapper>,
    IComparable<string>
{
    
    private readonly string _innerString = innerString;

    public StringWrapper(ReadOnlySpan<char> span) : this(new(span))
    {
        
    }

    public static StringWrapper Empty => string.Empty;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ListEnumerator<char, StringWrapper> GetEnumerator() => this.GetListEnumerator<char, StringWrapper>();
    
    IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _innerString.Length;
    }

    public char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _innerString[index];
    }

    public StringWrapper this[Range range] => _innerString[range];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringWrapper(string str) => new(str);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(StringWrapper str) => str._innerString;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other)
    {
        return _innerString == other;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(StringWrapper other)
    {
        return _innerString == other._innerString;
    }

    public int CompareTo(string? other, StringComparison comparisonType)
    {
        return string.Compare(_innerString, other, comparisonType);
    }

    public int CompareTo(StringWrapper other) => CompareTo(other._innerString, StringComparison.InvariantCulture);

    public int CompareTo(string? other) => CompareTo(other, StringComparison.InvariantCulture);

    public override bool Equals(object? obj)
    {
        return obj is StringWrapper other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return _innerString.GetHashCode();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(StringWrapper left, StringWrapper right)
    {
        return left.Equals(right);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(StringWrapper left, StringWrapper right)
    {
        return !(left == right);
    }

    public static string operator+(StringWrapper lhs, StringWrapper rhs) => lhs._innerString + rhs._innerString;
    
    public override string ToString() => _innerString;
}