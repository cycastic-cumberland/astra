using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Astra.TypeErasure.Data;

// 192 bytes down to 136. What a steal!
[StructLayout(LayoutKind.Explicit)]
public struct PackedDataCells : IEquatable<PackedDataCells>
{
    [FieldOffset(0)] 
    private byte _type0;
    [FieldOffset(1)]
    private byte _type1;
    [FieldOffset(2)]
    private byte _type2;
    [FieldOffset(3)]
    private byte _type3;
    [FieldOffset(4)]
    private byte _type4;
    [FieldOffset(5)]
    private byte _type5;
    [FieldOffset(6)]
    private byte _type6;
    [FieldOffset(7)]
    private byte _type7;
    [FieldOffset(8)]
    private long _raw0;
    [FieldOffset(16)]
    private long _raw1;
    [FieldOffset(24)]
    private long _raw2;
    [FieldOffset(32)]
    private long _raw3;
    [FieldOffset(40)]
    private long _raw4;
    [FieldOffset(48)]
    private long _raw5;
    [FieldOffset(56)]
    private long _raw6;
    [FieldOffset(64)]
    private long _raw7;
    [FieldOffset(72)]
    private object _ptr0;
    [FieldOffset(80)]
    private object _ptr1;
    [FieldOffset(88)]
    private object _ptr2;
    [FieldOffset(96)]
    private object _ptr3;
    [FieldOffset(104)]
    private object _ptr4;
    [FieldOffset(112)]
    private object _ptr5;
    [FieldOffset(120)]
    private object _ptr6;
    [FieldOffset(128)]
    private object _ptr7;

    private DataCell Cell0
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type0, _raw0, _ptr0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type0 = value.CellType;
            _raw0 = value.QWord;
            _ptr0 = value.Pointer;
        }
    }
    private DataCell Cell1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type1, _raw1, _ptr1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type1 = value.CellType;
            _raw1 = value.QWord;
            _ptr1 = value.Pointer;
        }
    }
    private DataCell Cell2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type2, _raw2, _ptr2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type2 = value.CellType;
            _raw2 = value.QWord;
            _ptr2 = value.Pointer;
        }
    }
    private DataCell Cell3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type3, _raw3, _ptr3);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type3 = value.CellType;
            _raw3 = value.QWord;
            _ptr3 = value.Pointer;
        }
    }
    private DataCell Cell4
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type4, _raw4, _ptr4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type4 = value.CellType;
            _raw4 = value.QWord;
            _ptr4 = value.Pointer;
        }
    }
    private DataCell Cell5
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type5, _raw5, _ptr5);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type5 = value.CellType;
            _raw5 = value.QWord;
            _ptr5 = value.Pointer;
        }
    }
    private DataCell Cell6
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type6, _raw6, _ptr6);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type6 = value.CellType;
            _raw6 = value.QWord;
            _ptr6 = value.Pointer;
        }
    }
    private DataCell Cell7
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_type7, _raw7, _ptr7);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _type7 = value.CellType;
            _raw7 = value.QWord;
            _ptr7 = value.Pointer;
        }
    }

    public DataCell this[int index]
    {
        get => unchecked((uint)index) switch
        {
            0U => Cell0,
            1U => Cell1,
            2U => Cell2,
            3U => Cell3,
            4U => Cell4,
            5U => Cell5,
            6U => Cell6,
            7U => Cell7,
            _ => throw new IndexOutOfRangeException()
        };
        set
        {
            switch (unchecked((uint)index))
            {
                case 0U:
                    Cell0 = value;
                    break;
                case 1U:
                    Cell1 = value;
                    break;
                case 2U:
                    Cell2 = value;
                    break;
                case 3U:
                    Cell3 = value;
                    break;
                case 4U:
                    Cell4 = value;
                    break;
                case 5U:
                    Cell5 = value;
                    break;
                case 6U:
                    Cell6 = value;
                    break;
                case 7U:
                    Cell7 = value;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(PackedDataCells other) => Equals(ref other);
    
    public bool Equals(ref readonly PackedDataCells other)
    {
        DataCell lhs;
        DataCell rhs;
        for (var i = 0; i < 8; i++)
        {
            lhs = this[i];
            rhs = other[i];
            if (lhs != rhs) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is PackedDataCells cells && Equals(ref cells);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(_type0);
        hashCode.Add(_type1);
        hashCode.Add(_type2);
        hashCode.Add(_type3);
        hashCode.Add(_type4);
        hashCode.Add(_type5);
        hashCode.Add(_type6);
        hashCode.Add(_type7);
        hashCode.Add(_raw0);
        hashCode.Add(_raw1);
        hashCode.Add(_raw2);
        hashCode.Add(_raw3);
        hashCode.Add(_raw4);
        hashCode.Add(_raw5);
        hashCode.Add(_raw6);
        hashCode.Add(_raw7);
        hashCode.Add(_ptr0);
        hashCode.Add(_ptr1);
        hashCode.Add(_ptr2);
        hashCode.Add(_ptr3);
        hashCode.Add(_ptr4);
        hashCode.Add(_ptr5);
        hashCode.Add(_ptr6);
        hashCode.Add(_ptr7);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(PackedDataCells left, PackedDataCells right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PackedDataCells left, PackedDataCells right)
    {
        return !(left == right);
    }
}