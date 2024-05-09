using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.Common.Data;

public readonly ref struct StringRef
{
    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<char> _chars;
        private int _index;

        public Enumerator(ReadOnlySpan<char> chars)
        {
            _chars = chars;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++_index < _chars.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            
        }

        public char Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _chars[_index];
        }
    }
    private readonly ReadOnlySpan<char> _chars;

    public StringRef(ReadOnlySpan<char> chars)
    {
        _chars = chars;
    }

    public char this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars[index];
    }
    
    public ReadOnlySpan<char> this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars[range];
    }

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _chars.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        return new(_chars);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan() => _chars;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<char>(StringRef stringRef) => stringRef._chars;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => new(_chars);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => unchecked((int)XxHash32.HashToUInt32(MemoryMarshal.Cast<char, byte>(_chars)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(StringRef left, StringRef right)
    {
        return BytesComparisonHelper.Equals(MemoryMarshal.Cast<char, byte>(left._chars),
            MemoryMarshal.Cast<char, byte>(right._chars));
    }

    public static bool operator !=(StringRef left, StringRef right)
    {
        return !(left == right);
    }
}