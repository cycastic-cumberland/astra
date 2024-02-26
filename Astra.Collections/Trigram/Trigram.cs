namespace Astra.Collections.Trigram;

[System.Runtime.CompilerServices.InlineArray(3)]
public struct TrigramBuilder<T>
    where T : IEquatable<T>
{
    private T _element0;

    public static TrigramBuilder<T> Create()
    {
        return new();
    }
    
    public Trigram<T> ToTrigram(int length)
    {
        return new(this, length);
    }
}

file static class TrigramHelpers
{
    public static string ToStringChars(Trigram<char> trigram)
    {
        Span<char> span = stackalloc char[3];
        span[0] = trigram.Element0;
        span[1] = trigram.Element1;
        span[2] = trigram.Element2;
        return new(span);
    }
    
    public static string ToStringGenerics<T>(Trigram<T> trigram) where T : IEquatable<T>
    {
        return $"Trigram<{typeof(T).Name}>[{trigram.Length}]";
    }
}

public readonly struct Trigram<T> : IEquatable<Trigram<T>>
    where T : IEquatable<T>
{
    private static readonly Func<Trigram<T>, string> ToStringMethod = typeof(T) == typeof(char)
        ? (Func<Trigram<T>, string>)(object)TrigramHelpers.ToStringChars
        : TrigramHelpers.ToStringGenerics;

    private readonly T _element0;
    private readonly T _element1;
    private readonly T _element2;
    private readonly int _length;
    private readonly int _hash;

    public Trigram(TrigramBuilder<T> builder, int length)
    {
        _element0 = builder[0];
        _element1 = builder[1];
        _element2 = builder[2];
        _length = length;
        _hash = HashCode.Combine(_element0, _element1, _element2);
    }

    public T Element0 => _element0;
    public T Element1 => _element1;
    public T Element2 => _element2;
    public int Length => _length;

    public bool Equals(Trigram<T> other)
    {
        return    _element0.Equals(other._element0)
               && _element1.Equals(other._element1)
               && _element2.Equals(other._element2);
    }
    
    public bool NotEquals(Trigram<T> other)
    {
        return      !_element0.Equals(other._element0)
                 || !_element1.Equals(other._element1)
                 || !_element2.Equals(other._element2);
    }

    public override bool Equals(object? obj)
    {
        return obj is Trigram<T> other && Equals(other);
    }

    public override int GetHashCode() => _hash;

    public static bool operator ==(Trigram<T> left, Trigram<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Trigram<T> left, Trigram<T> right)
    {
        return left.NotEquals(right);
    }

    public override string ToString() => ToStringMethod(this);
}

