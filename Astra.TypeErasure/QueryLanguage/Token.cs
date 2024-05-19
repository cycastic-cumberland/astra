using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.QueryLanguage;

[StructLayout(LayoutKind.Explicit)]
public struct Token
{
    public static class TokenTypes
    {
        public const byte Keyword       = 1;
        public const byte Number        = 2;
        public const byte Symbol        = 3;
        public const byte Operator      = 4;
        public const byte String        = 5;
        public const byte Delimiter     = 6;
    }
    [FieldOffset(0)] 
    private byte _tokenType;
    [FieldOffset(1)] 
    private byte _operatorCode;
    [FieldOffset(2)] 
    private byte _cellCode;
    [FieldOffset(4)] 
    private int _loc;
    [FieldOffset(8)] 
    private long _raw;
    [FieldOffset(16)] 
    private object _ptr;

    public override int GetHashCode()
    {
        return HashCode.Combine(_tokenType, _operatorCode, _cellCode, _loc, _raw, _ptr);
    }

    public byte TokenType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _tokenType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _tokenType = value;
    }

    public byte Operator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _operatorCode;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _operatorCode = value;
    }
    
    public int Location
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _loc;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _loc = value;
    }

    public DataCell Storage
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_cellCode, _raw, _ptr);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _cellCode = value.CellType;
            _raw = value.DWord;
            _ptr = value.Pointer;
        }
    }
}