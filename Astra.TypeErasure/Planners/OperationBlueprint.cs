using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Astra.Common.Data;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Data;

namespace Astra.TypeErasure.Planners;

[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct OperationBlueprint
{
    [FieldOffset(0)] 
    private byte _queryOperationType;
    [FieldOffset(1)] 
    private byte _predicateOperationType;
    [FieldOffset(2)] 
    private byte _cell1;
    [FieldOffset(3)] 
    private byte _cell2;
    [FieldOffset(4)] 
    private int _offset;
    [FieldOffset(8)]
    private long _raw1;
    [FieldOffset(16)]
    private long _raw2;
    [FieldOffset(24)]
    private object _ptr1;
    [FieldOffset(32)]
    private object _ptr2;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_queryOperationType);
        hash.Add(_predicateOperationType);
        hash.Add(_cell1);
        hash.Add(_cell2);
        hash.Add(_offset);
        hash.Add(_raw1);
        hash.Add(_raw2);
        hash.Add(_ptr1);
        hash.Add(_ptr2);
        return hash.ToHashCode();
    }

    public uint QueryOperationType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _queryOperationType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _queryOperationType = unchecked((byte)value);
    }
    public uint PredicateOperationType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _predicateOperationType;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _predicateOperationType = unchecked((byte)value);
    }
    public int Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _offset;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _offset = value;
    }
    public DataCell Cell1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_cell1, _raw1, _ptr1);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _cell1 = value.CellType;
            _raw1 = value.DWord;
            _ptr1 = value.Pointer;
        }
    }
    public DataCell Cell2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_cell2, _raw2, _ptr2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _cell2 = value.CellType;
            _raw2 = value.DWord;
            _ptr2 = value.Pointer;
        }
    }

    public static void ToStream<T>(ref readonly OperationBlueprint self, ref readonly T stream) where T : IStreamWrapper
    {
        switch ((uint)self._queryOperationType)
        {
            case QueryType.IntersectMask:
            {
                stream.SaveValue(QueryType.IntersectMask);
                break;
            }
            case QueryType.UnionMask:
            {
                stream.SaveValue(QueryType.UnionMask);
                break;
            }
            case QueryType.FilterMask:
            {
                stream.SaveValue(QueryType.FilterMask);
                stream.SaveValue((uint)self._offset);
                stream.SaveValue((uint)self._predicateOperationType);
                var cell = self.Cell1;
                stream.SaveValue(cell.DataType.Value);
                cell.Write(in stream);
                break;
            }
            default:
                throw new AggregateException($"Operation type type not supported: {self._queryOperationType}");
        }
    }
}
