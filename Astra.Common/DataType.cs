using System.Runtime.CompilerServices;

namespace Astra.Common;

public class IncompatibleDataTypeException(string? msg) : Exception(msg)
{
    public IncompatibleDataTypeException() : this(null) {}
}

public class DataTypeNotSupportedException(string? msg) : Exception(msg)
{
    public DataTypeNotSupportedException() : this(null) {}
}

public static class DataTypeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DataType AstraDataType(this uint value) => new(value);
}

public readonly struct DataType(uint value)
{
    public override bool Equals(object? obj)
    {
        return obj is DataType other && this == other;
    }

    public override int GetHashCode() => value.GetHashCode();

    public const uint NoneMask         = 0U;

    public const uint SerialMask       = AutoIncrement | DWordMask;
    public const uint BigSerialMask    = AutoIncrement | QWordMask;
    public const uint ByteMask         = 1U << 12 | Comparable | Numeric | StaticSize;
    public const uint WordMask         = 1U << 13 | Comparable | Numeric | StaticSize;
    public const uint DWordMask        = 1U << 14 | Comparable | Numeric | StaticSize;
    public const uint QWordMask        = 1U << 15 | Comparable | Numeric | StaticSize;
    public const uint SingleMask       = 1U << 16 | Comparable | Numeric | StaticSize;
    public const uint DoubleMask       = 1U << 17 | Comparable | Numeric | StaticSize;
    public const uint StringMask       = 1U << 18 | Comparable;
    public const uint BytesMask        = 1U << 19;

    public const uint Comparable       = 1U << 31;
    public const uint Numeric          = 1U << 30;
    public const uint StaticSize       = 1U << 29;
    public const uint AutoIncrement    = 1U << 28;

    public uint Value => value;
    
    public static readonly  DataType None = new(NoneMask);
    public static readonly  DataType Serial = new(SerialMask);
    public static readonly  DataType BigSerial = new(BigSerialMask);
    public static readonly  DataType Byte = new(ByteMask);
    public static readonly  DataType Word = new(WordMask);
    public static readonly  DataType DWord = new(DWordMask);
    public static readonly  DataType QWord = new(QWordMask);
    public static readonly  DataType Single = new(SingleMask);
    public static readonly  DataType Double = new(DoubleMask);
    public static readonly  DataType String = new(StringMask);
    public static readonly  DataType Bytes = new(BytesMask);

    public bool IsNone => value == 0;
    public bool IsSerial => value == SerialMask;
    public bool IsBigSerial => value == BigSerialMask;
    public bool IsByte => (value | ByteMask) != 0;
    public bool IsWord => (value | WordMask) != 0;
    public bool IsDWord => (value | DWordMask) != 0;
    public bool IsQWord => (value | QWordMask) != 0;
    public bool IsSingle => (value | SingleMask) != 0;
    public bool IsDouble => (value | DoubleMask) != 0;
    public bool IsString => (value | StringMask) != 0;
    public bool IsBytes => (value | BytesMask) != 0;
    public bool IsComparable => (value | Comparable) != 0;
    public bool IsNumeric => (value | Numeric) != 0;
    public bool IsStaticSize => (value | StaticSize) != 0;
    public bool IsAutoIncrement => (value | AutoIncrement) != 0;
    
    public static explicit operator uint(DataType self) => self.Value;

    public static bool operator==(DataType lhs, DataType rhs)
    {
        return lhs.Value == rhs.Value;
    }
    
    public static bool operator!=(DataType lhs, DataType rhs)
    {
        return lhs.Value != rhs.Value;
    }
}