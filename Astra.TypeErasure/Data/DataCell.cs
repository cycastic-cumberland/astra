using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.StreamUtils;

namespace Astra.TypeErasure.Data;

[StructLayout(LayoutKind.Explicit)]
[DebuggerDisplay("Value = {Value}, Type = {TypeString}")]
public readonly struct DataCell : INumber<DataCell>, IDisposable
{
    public static readonly DataCell MinValue = new(double.MinValue);
    public static readonly DataCell MaxValue = new(double.MaxValue);
    public static class CellTypes
    {
        public const byte Unset     = 0;
        public const byte DWord     = 2;
        public const byte QWord     = 3;
        public const byte Single    = 4;
        public const byte Double    = 5;
        public const byte Text      = 6;
        public const byte Bytes     = 7;
    }
    
    [FieldOffset(0)] 
    public readonly byte CellType;
    [FieldOffset(8)]
    public readonly int DWord;
    [FieldOffset(8)]
    public readonly long QWord;
    [FieldOffset(8)]
    public readonly float Single;
    [FieldOffset(8)]
    public readonly double Double;
    [FieldOffset(16)] 
    public readonly object Pointer;

    public Type Type => Value.GetType();
    private string TypeString => Type.ToString();
    
    public object Value => CellType switch
    {
        CellTypes.Unset => unchecked((nuint)QWord),
        CellTypes.DWord => DWord,
        CellTypes.QWord => QWord,
        CellTypes.Single => Single,
        CellTypes.Double => Double,
        CellTypes.Text => ExtractText().ToString(),
        CellTypes.Bytes => ExtractBytes().ToArray(),
        _ => throw new ArgumentOutOfRangeException()
    };

    public DataType DataType => TypeToAstraDataType(CellType);
    
    public static DataType TypeToAstraDataType(byte cellType) => cellType switch
    {
        CellTypes.DWord => DataType.DWord,
        CellTypes.QWord => DataType.QWord,
        CellTypes.Single => DataType.Single,
        CellTypes.Double => DataType.Double,
        CellTypes.Text => DataType.String,
        CellTypes.Bytes => DataType.Bytes,
        _ => throw new ArgumentOutOfRangeException()
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DataCell(byte type, long raw, object? pointer)
    {
        CellType = type;
        QWord = raw;
        Pointer = pointer!;
    }
    
    public DataCell(int value) 
    {
        Pointer = null!;
        DWord = value;
        CellType = CellTypes.DWord;
    }
    
    public DataCell(long value) 
    {
        Pointer = null!;
        QWord = value;
        CellType = CellTypes.QWord;
    }
    
    public DataCell(float value) 
    {
        Pointer = null!;
        Single = value;
        CellType = CellTypes.Single;
    }
    
    public DataCell(double value) 
    {
        Pointer = null!;
        Double = value;
        CellType = CellTypes.Double;
    }
    
    public DataCell(string value) : this((ReadOnlySpan<char>)value)
    {
        
    }
    
    public DataCell(ReadOnlySpan<char> value)
    {
        var buffer = ArrayPool<char>.Shared.Rent(value.Length);
        var length = value.Length;
        try
        {
            unsafe
            {
                fixed (void* dest = buffer, source = value)
                {
                    Buffer.MemoryCopy(source, dest, buffer.Length * sizeof(char), length * sizeof(char));
                }
            }
        }
        catch
        {
            ArrayPool<char>.Shared.Return(buffer);
            throw;
        }
        
        Pointer = buffer;
        DWord = length;
        CellType = CellTypes.Text;
    }
    
    private DataCell(int length, char[] buffer)
    {
        Pointer = buffer;
        DWord = length;
        CellType = CellTypes.Text;
    }
    
    public DataCell(ReadOnlySpan<byte> value)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(value.Length);
        var length = value.Length;
        try
        {
            unsafe
            {
                fixed (void* dest = buffer, source = value)
                {
                    Buffer.MemoryCopy(source, dest, buffer.Length * sizeof(byte), length * sizeof(char));
                }
            }
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }

        Pointer = buffer;
        DWord = length;
        CellType = CellTypes.Bytes;
    }

    public DataCell(byte[] value) : this((ReadOnlySpan<byte>)value)
    {
        
    }
    
    private DataCell(int length, byte[] value)
    {
        Pointer = value;
        DWord = length;
        CellType = CellTypes.Bytes;
    }

    public override string ToString()
    {
        return CellType switch
        {
            CellTypes.DWord => DWord.ToString(),
            CellTypes.QWord => QWord.ToString(),
            CellTypes.Single => Single.ToString(CultureInfo.InvariantCulture),
            CellTypes.Double => Double.ToString(CultureInfo.InvariantCulture),
            CellTypes.Text => ExtractText().ToString(),
            CellTypes.Bytes => ExtractBytes().ToHexString(),
            _ => string.Empty
        };
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        switch (CellType)
        {
            case CellTypes.DWord:
                return DWord.TryFormat(destination, out charsWritten, format, provider);
            case CellTypes.QWord:
                return QWord.TryFormat(destination, out charsWritten, format, provider);
            case CellTypes.Single:
                return Single.TryFormat(destination, out charsWritten, format, provider);
            case CellTypes.Double:
                return Double.TryFormat(destination, out charsWritten, format, provider);
            case CellTypes.Text:
            {
                var str = ExtractText();
                if (destination.Length < str.Length)
                {
                    charsWritten = 0;
                    return false;
                }
                charsWritten = str.Length;
                str.AsSpan().CopyTo(destination);
                return true;
            }
            case CellTypes.Bytes:
            {
                var bytes = ExtractBytes();
                if (destination.Length < bytes.Length * 2)
                {
                    charsWritten = 0;
                    return false;
                }

                bytes.ToHexStringUpper(destination);
                charsWritten = bytes.Length * 2;
                return true;
            }
            default:
                throw new NotSupportedException($"Unsupported type: {CellType}");
        }
    }

    public int CompareTo(object? obj)
    {
        if (obj is not DataCell cell) throw new ArgumentException(nameof(obj));
        return CompareTo(cell);
    }

    // Unimplemented: byte, char
    private int CompareDWord(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => DWord.CompareTo(other.DWord),
            CellTypes.QWord => DWord.CompareTo(unchecked((int)other.QWord)),
            CellTypes.Single => ((float)DWord).CompareTo(other.Single),
            CellTypes.Double => ((double)DWord).CompareTo(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private int CompareQWord(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => QWord.CompareTo(other.DWord),
            CellTypes.QWord => QWord.CompareTo(other.QWord),
            CellTypes.Single => ((double)QWord).CompareTo(other.Single),
            CellTypes.Double => ((double)QWord).CompareTo(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private int CompareSingle(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => Single.CompareTo(other.DWord),
            CellTypes.QWord => Single.CompareTo(other.QWord),
            CellTypes.Single => Single.CompareTo(other.Single),
            CellTypes.Double => Single.CompareTo((float)other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private int CompareDouble(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => Double.CompareTo(other.DWord),
            CellTypes.QWord => Double.CompareTo(other.QWord),
            CellTypes.Single => Double.CompareTo(other.Single),
            CellTypes.Double => Double.CompareTo(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private int CompareText(ref readonly DataCell other)
    {
        if (other.CellType == CellTypes.Text)
            return ExtractText() == other.ExtractText() ? 0 : -1;
        throw new MismatchedDataTypeException();
    }
    
    private int CompareBytes(ref readonly DataCell other)
    {
        if (other.CellType == CellTypes.Bytes)
            return BytesComparisonHelper.Equals(ExtractBytes(), other.ExtractBytes()) ? 0 : -1;
        throw new MismatchedDataTypeException();
    }
    
    private int CompareTo(ref readonly DataCell other)
    {
        return CellType switch
        {
            CellTypes.DWord => CompareDWord(in other),
            CellTypes.QWord => CompareQWord(in other),
            CellTypes.Single => CompareSingle(in other),
            CellTypes.Double => CompareDouble(in other),
            CellTypes.Text => CompareText(in other),
            CellTypes.Bytes => CompareBytes(in other),
            _ => throw new UnreachableException($"Unsupported cell type: {CellType}")
        };
    }

    public int CompareTo(DataCell other) => CompareTo(ref other);
    
    private bool EqualsDWord(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => DWord == other.DWord,
            CellTypes.QWord => DWord == other.QWord,
            CellTypes.Single => ((float)DWord).Equals(other.Single),
            CellTypes.Double => ((double)DWord).Equals(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private bool EqualsQWord(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => QWord == other.DWord,
            CellTypes.QWord => QWord == other.QWord,
            CellTypes.Single => ((double)QWord).Equals(other.Single),
            CellTypes.Double => ((double)QWord).Equals(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private bool EqualsSingle(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => Single.Equals(other.DWord),
            CellTypes.QWord => Single.Equals(other.QWord),
            CellTypes.Single => Single.Equals(other.Single),
            CellTypes.Double => Single.Equals((float)other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private bool EqualsDouble(ref readonly DataCell other)
    {
        return other.CellType switch
        {
            CellTypes.DWord => Double.Equals(other.DWord),
            CellTypes.QWord => Double.Equals(other.QWord),
            CellTypes.Single => Double.Equals(other.Single),
            CellTypes.Double => Double.Equals(other.Double),
            CellTypes.Text => throw new MismatchedDataTypeException(),
            CellTypes.Bytes => throw new MismatchedDataTypeException(),
            _ => throw new MismatchedDataTypeException()
        };
    }
    
    private bool EqualsText(ref readonly DataCell other)
    {
        if (other.CellType == CellTypes.Text)
            return ExtractText() == other.ExtractText();
        throw new MismatchedDataTypeException();
    }
    
    private bool EqualsBytes(ref readonly DataCell other)
    {
        if (other.CellType == CellTypes.Bytes)
            return BytesComparisonHelper.Equals(ExtractBytes(), other.ExtractBytes());
        throw new MismatchedDataTypeException();
    }
    
    public bool Equals(ref readonly DataCell other)
    {
        return CellType switch
        {
            CellTypes.DWord => EqualsDWord(in other),
            CellTypes.QWord => EqualsQWord(in other),
            CellTypes.Single => EqualsSingle(in other),
            CellTypes.Double => EqualsDouble(in other),
            CellTypes.Text => EqualsText(in other),
            CellTypes.Bytes => EqualsBytes(in other),
            _ => throw new UnreachableException($"Unsupported cell type: {CellType}")
        };
    }

    public bool Equals(DataCell other) => Equals(ref other);

    public static DataCell operator %(DataCell left, DataCell right)
    {
        if (left.CellType != right.CellType) throw new MismatchedDataTypeException();
        return left.CellType switch
        {
            CellTypes.DWord => new DataCell(left.DWord % right.DWord),
            CellTypes.QWord => new DataCell(left.QWord % right.QWord),
            _ => throw new UnreachableException($"Unsupported cell type: {left.CellType}")
        };
    }

    public static DataCell operator +(DataCell value) => value;

    public override bool Equals(object? obj)
    {
        return obj is DataCell cell && Equals(cell);
    }

    public override int GetHashCode()
    {
        return CellType switch
        {
            CellTypes.DWord => ((double)DWord).GetHashCode(),
            CellTypes.QWord => ((double)QWord).GetHashCode(),
            CellTypes.Single => ((double)Single).GetHashCode(),
            CellTypes.Double => Double.GetHashCode(),
            CellTypes.Text => ExtractText().GetHashCode(),
            CellTypes.Bytes => unchecked((int)XxHash32.HashToUInt32(ExtractBytes())),
            _ => throw new UnreachableException($"Unsupported cell type: {CellType}")
        };
    }

    public void WriteFull<T>(T stream) where T : IStreamWrapper
    {
        switch (CellType)
        {
            case CellTypes.DWord:
                stream.SaveValue(DataType.DWordMask);
                stream.SaveValue(DWord);
                return;
            case CellTypes.QWord:
                stream.SaveValue(DataType.QWordMask);
                stream.SaveValue(QWord);
                return;
            case CellTypes.Single:
                stream.SaveValue(DataType.SingleMask);
                stream.SaveValue(Single);
                return;
            case CellTypes.Double:
                stream.SaveValue(DataType.DoubleMask);
                stream.SaveValue(Double);
                return;
            case CellTypes.Text:
                stream.SaveValue(DataType.StringMask);
                stream.SaveValue(ExtractText());
                return;
            case CellTypes.Bytes:
                stream.SaveValue(DataType.BytesMask);
                stream.SaveValue(ExtractBytes());
                return;
        }
    }
    
    public void Write(Stream stream)
    {
        switch (CellType)
        {
            case CellTypes.DWord:
                stream.WriteValue(DWord);
                return;
            case CellTypes.QWord:
                stream.WriteValue(QWord);
                return;
            case CellTypes.Single:
                stream.WriteValue(Single);
                return;
            case CellTypes.Double:
                stream.WriteValue(Double);
                return;
            case CellTypes.Text:
            {
                stream.WriteValue(ExtractText());
                return;
            }
            case CellTypes.Bytes:
            {
                stream.WriteValue(ExtractBytes());
                return;
            }
        }
    }
    
    public void Write<T>(ref readonly T stream) where T : IStreamWrapper
    {
        switch (CellType)
        {
            case CellTypes.DWord:
                stream.SaveValue(DWord);
                return;
            case CellTypes.QWord:
                stream.SaveValue(QWord);
                return;
            case CellTypes.Single:
                stream.SaveValue(Single);
                return;
            case CellTypes.Double:
                stream.SaveValue(Double);
                return;
            case CellTypes.Text:
            {
                stream.SaveValue(ExtractText());
                return;
            }
            case CellTypes.Bytes:
            {
                stream.SaveValue(ExtractBytes());
                return;
            }
        }
    }

    public static bool operator ==(DataCell left, DataCell right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DataCell left, DataCell right)
    {
        return !(left == right);
    }

    public static DataCell FromStream<T>(T stream) where T : IStreamWrapper
    {
        var typeCode = stream.LoadUInt();
        switch (typeCode)
        {
            case DataType.DWordMask:
                return new(stream.LoadInt());
            case DataType.QWordMask:
                return new(stream.LoadLong());
            case DataType.SingleMask:
                return new(stream.LoadSingle());
            case DataType.DoubleMask:
                return new(stream.LoadBytes());
            case DataType.StringMask:
            {
                var (length, buffer) = stream.LoadStringToBuffer();
                return new(length, buffer);
            }
            case DataType.BytesMask:
            {
                var (length, buffer) = stream.LoadBytesToBuffer();
                return new(length, buffer);
            }
            default:
                throw new NotSupportedException($"Unsupported type: {typeCode}");
        }
    }
    
    public static DataCell FromStream(uint typeCode, Stream stream)
    {
        switch (typeCode)
        {
            case DataType.DWordMask:
                return new(stream.ReadInt());
            case DataType.QWordMask:
                return new(stream.ReadLong());
            case DataType.SingleMask:
                return new(stream.ReadSingle());
            case DataType.DoubleMask:
                return new(stream.ReadDouble());
            case DataType.StringMask:
            {
                var (length, buffer) = stream.ReadStringToBuffer();
                return new(length, buffer);
            }
            case DataType.BytesMask:
            {
                var (length, buffer) = stream.ReadSequenceToBuffer();
                return new(length, buffer);
            }
            default:
                throw new NotSupportedException($"Unsupported type: {typeCode}");
        }
    }
    
    public static DataCell Parse(string s, IFormatProvider? provider)
    {
        return Parse(s.AsSpan(), provider);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out DataCell result)
    {
        return TryParse(s.AsSpan(), provider, out result);
    }

    public static DataCell Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (long.TryParse(s, out var i64)) return new(i64);
        if (double.TryParse(s, out var f64)) return new(f64);
        throw new FormatException();
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DataCell result)
    {
        if (long.TryParse(s, out var i64))
        {
            result = new(i64);
            return true;
        }
        if (double.TryParse(s, out var f64))
        {
            result = new(f64);
            return true;
        }

        result = new();
        return false;
    }

    private static DataCell AddDWord(int lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs + rhs.DWord),
            CellTypes.QWord => new DataCell(lhs + rhs.QWord),
            CellTypes.Single => new DataCell(lhs + rhs.Single),
            CellTypes.Double => new DataCell(lhs + rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell AddQWord(long lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs + rhs.DWord),
            CellTypes.QWord => new DataCell(lhs + rhs.QWord),
            CellTypes.Single => new DataCell(lhs + rhs.Single),
            CellTypes.Double => new DataCell(lhs + rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell AddSingle(float lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs + rhs.DWord),
            CellTypes.QWord => new DataCell(lhs + rhs.QWord),
            CellTypes.Single => new DataCell(lhs + rhs.Single),
            CellTypes.Double => new DataCell(lhs + rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell AddDouble(double lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs + rhs.DWord),
            CellTypes.QWord => new DataCell(lhs + rhs.QWord),
            CellTypes.Single => new DataCell(lhs + rhs.Single),
            CellTypes.Double => new DataCell(lhs + rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell AddText(string lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs + rhs.DWord),
            CellTypes.QWord => new DataCell(lhs + rhs.QWord),
            CellTypes.Single => new DataCell(lhs + rhs.Single),
            CellTypes.Double => new DataCell(lhs + rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell SubDWord(int lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs - rhs.DWord),
            CellTypes.QWord => new DataCell(lhs - rhs.QWord),
            CellTypes.Single => new DataCell(lhs - rhs.Single),
            CellTypes.Double => new DataCell(lhs - rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell SubQWord(long lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs - rhs.DWord),
            CellTypes.QWord => new DataCell(lhs - rhs.QWord),
            CellTypes.Single => new DataCell(lhs - rhs.Single),
            CellTypes.Double => new DataCell(lhs - rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell SubSingle(float lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs - rhs.DWord),
            CellTypes.QWord => new DataCell(lhs - rhs.QWord),
            CellTypes.Single => new DataCell(lhs - rhs.Single),
            CellTypes.Double => new DataCell(lhs - rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell SubDouble(double lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs - rhs.DWord),
            CellTypes.QWord => new DataCell(lhs - rhs.QWord),
            CellTypes.Single => new DataCell(lhs - rhs.Single),
            CellTypes.Double => new DataCell(lhs - rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell MulDWord(int lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs * rhs.DWord),
            CellTypes.QWord => new DataCell(lhs * rhs.QWord),
            CellTypes.Single => new DataCell(lhs * rhs.Single),
            CellTypes.Double => new DataCell(lhs * rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell MulQWord(long lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs * rhs.DWord),
            CellTypes.QWord => new DataCell(lhs * rhs.QWord),
            CellTypes.Single => new DataCell(lhs * rhs.Single),
            CellTypes.Double => new DataCell(lhs * rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell MulSingle(float lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs * rhs.DWord),
            CellTypes.QWord => new DataCell(lhs * rhs.QWord),
            CellTypes.Single => new DataCell(lhs * rhs.Single),
            CellTypes.Double => new DataCell(lhs * rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell MulDouble(double lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs * rhs.DWord),
            CellTypes.QWord => new DataCell(lhs * rhs.QWord),
            CellTypes.Single => new DataCell(lhs * rhs.Single),
            CellTypes.Double => new DataCell(lhs * rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell DivDWord(int lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs / rhs.DWord),
            CellTypes.QWord => new DataCell(lhs / rhs.QWord),
            CellTypes.Single => new DataCell(lhs / rhs.Single),
            CellTypes.Double => new DataCell(lhs / rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell DivQWord(long lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs / rhs.DWord),
            CellTypes.QWord => new DataCell(lhs / rhs.QWord),
            CellTypes.Single => new DataCell(lhs / rhs.Single),
            CellTypes.Double => new DataCell(lhs / rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell DivSingle(float lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs / rhs.DWord),
            CellTypes.QWord => new DataCell(lhs / rhs.QWord),
            CellTypes.Single => new DataCell(lhs / rhs.Single),
            CellTypes.Double => new DataCell(lhs / rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }
    
    private static DataCell DivDouble(double lhs, ref readonly DataCell rhs)
    {
        return rhs.CellType switch
        {
            CellTypes.DWord => new DataCell(lhs / rhs.DWord),
            CellTypes.QWord => new DataCell(lhs / rhs.QWord),
            CellTypes.Single => new DataCell(lhs / rhs.Single),
            CellTypes.Double => new DataCell(lhs / rhs.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {rhs.CellType}")
        };
    }

    public static DataCell operator +(DataCell left, DataCell right)
    {
        return left.CellType switch
        {
            CellTypes.DWord => AddDWord(left.DWord, ref right),
            CellTypes.QWord => AddQWord(left.QWord, ref right),
            CellTypes.Single => AddSingle(left.Single, ref right),
            CellTypes.Double => AddDouble(left.Double, ref right),
            CellTypes.Text => AddText((string)left.Pointer, ref right),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {left.CellType}")
        };
    }

    public static DataCell AdditiveIdentity => new();
    public static bool operator >(DataCell left, DataCell right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(DataCell left, DataCell right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <(DataCell left, DataCell right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(DataCell left, DataCell right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static DataCell operator --(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => new DataCell(value.DWord - 1),
            CellTypes.QWord => new DataCell(value.QWord - 1),
            CellTypes.Single => new DataCell(value.Single - 1),
            CellTypes.Double => new DataCell(value.Double - 1),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {value.CellType}")
        };
    }

    public static DataCell operator /(DataCell left, DataCell right)
    {
        return left.CellType switch
        {
            CellTypes.DWord => DivDWord(left.DWord, ref right),
            CellTypes.QWord => DivQWord(left.QWord, ref right),
            CellTypes.Single => DivSingle(left.Single, ref right),
            CellTypes.Double => DivDouble(left.Double, ref right),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {left.CellType}")
        };
    }

    public static DataCell operator ++(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => new DataCell(value.DWord + 1),
            CellTypes.QWord => new DataCell(value.QWord + 1),
            CellTypes.Single => new DataCell(value.Single + 1),
            CellTypes.Double => new DataCell(value.Double + 1),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {value.CellType}")
        };
    }

    public static DataCell MultiplicativeIdentity => new();
    public static DataCell operator *(DataCell left, DataCell right)
    {
        return left.CellType switch
        {
            CellTypes.DWord => MulDWord(left.DWord, ref right),
            CellTypes.QWord => MulQWord(left.QWord, ref right),
            CellTypes.Single => MulSingle(left.Single, ref right),
            CellTypes.Double => MulDouble(left.Double, ref right),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {left.CellType}")
        };
    }

    public static DataCell operator -(DataCell left, DataCell right)
    {
        return left.CellType switch
        {
            CellTypes.DWord => SubDWord(left.DWord, ref right),
            CellTypes.QWord => SubQWord(left.QWord, ref right),
            CellTypes.Single => SubSingle(left.Single, ref right),
            CellTypes.Double => SubDouble(left.Double, ref right),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {left.CellType}")
        };
    }

    public static DataCell operator -(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => new DataCell(-value.DWord),
            CellTypes.QWord => new DataCell(-value.QWord),
            CellTypes.Single => new DataCell(-value.Single),
            CellTypes.Double => new DataCell(-value.Double),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {value.CellType}")
        };
    }

    public static DataCell Abs(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => new DataCell(Math.Abs(value.DWord)),
            CellTypes.QWord => new DataCell(Math.Abs(value.QWord)),
            CellTypes.Single => new DataCell(Math.Abs(value.Single)),
            CellTypes.Double => new DataCell(Math.Abs(value.Double)),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new UnreachableException($"Unsupported cell type: {value.CellType}")
        };
    }

    public static bool IsCanonical(DataCell value) => true;

    public static bool IsComplexNumber(DataCell value) => false;

    public static bool IsEvenInteger(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => int.IsEvenInteger(value.DWord),
            CellTypes.QWord => long.IsEvenInteger(value.QWord),
            CellTypes.Single => float.IsEvenInteger(value.Single),
            CellTypes.Double => double.IsEvenInteger(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsFinite(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => true,
            CellTypes.QWord => true,
            CellTypes.Single => float.IsFinite(value.Single),
            CellTypes.Double => double.IsFinite(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsImaginaryNumber(DataCell value) => false;

    public static bool IsInfinity(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => false,
            CellTypes.QWord => false,
            CellTypes.Single => float.IsInfinity(value.Single),
            CellTypes.Double => double.IsInfinity(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsInteger(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => true,
            CellTypes.QWord => true,
            CellTypes.Single => float.IsInteger(value.Single),
            CellTypes.Double => double.IsInteger(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsNaN(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => false,
            CellTypes.QWord => false,
            CellTypes.Single => float.IsNaN(value.Single),
            CellTypes.Double => double.IsNaN(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsNegative(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => int.IsNegative(value.DWord),
            CellTypes.QWord => long.IsNegative(value.QWord),
            CellTypes.Single => float.IsNegative(value.Single),
            CellTypes.Double => double.IsNegative(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsNegativeInfinity(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => false,
            CellTypes.QWord => false,
            CellTypes.Single => float.IsNegativeInfinity(value.Single),
            CellTypes.Double => double.IsNegativeInfinity(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsNormal(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => value.DWord != 0,
            CellTypes.QWord => value.DWord != 0L,
            CellTypes.Single => float.IsNormal(value.Single),
            CellTypes.Double => double.IsNormal(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsOddInteger(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => int.IsOddInteger(value.DWord),
            CellTypes.QWord => long.IsOddInteger(value.QWord),
            CellTypes.Single => float.IsOddInteger(value.Single),
            CellTypes.Double => double.IsOddInteger(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsPositive(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => int.IsPositive(value.DWord),
            CellTypes.QWord => long.IsPositive(value.QWord),
            CellTypes.Single => float.IsPositive(value.Single),
            CellTypes.Double => double.IsPositive(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsPositiveInfinity(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => false,
            CellTypes.QWord => false,
            CellTypes.Single => float.IsPositiveInfinity(value.Single),
            CellTypes.Double => double.IsPositiveInfinity(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsRealNumber(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => true,
            CellTypes.QWord => true,
            CellTypes.Single => float.IsRealNumber(value.Single),
            CellTypes.Double => double.IsRealNumber(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsSubnormal(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => false,
            CellTypes.QWord => false,
            CellTypes.Single => float.IsSubnormal(value.Single),
            CellTypes.Double => double.IsSubnormal(value.Double),
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static bool IsZero(DataCell value)
    {
        return value.CellType switch
        {
            CellTypes.DWord => value.DWord == 0,
            CellTypes.QWord => value.DWord == 0L,
            CellTypes.Single => value.Single == 0.0f,
            CellTypes.Double => value.Double == 0.0,
            CellTypes.Text => false,
            CellTypes.Bytes => false,
            _ => false
        };
    }

    public static DataCell MaxMagnitude(DataCell x, DataCell y)
    {
        if (x.CellType != y.CellType) throw new MismatchedDataTypeException();
        return x.CellType switch
        {
            CellTypes.DWord => new DataCell(int.MaxMagnitude(x.DWord, y.DWord)),
            CellTypes.QWord => new DataCell(long.MaxMagnitude(x.QWord, y.QWord)),
            CellTypes.Single => new DataCell(float.MaxMagnitude(x.Single, y.Single)),
            CellTypes.Double => new DataCell(double.MaxMagnitude(x.Double, y.Double)),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };
    }

    public static DataCell MaxMagnitudeNumber(DataCell x, DataCell y)
    {
        if (x.CellType != y.CellType) throw new MismatchedDataTypeException();
        return x.CellType switch
        {
            CellTypes.DWord => new DataCell(int.MaxMagnitude(x.DWord, y.DWord)),
            CellTypes.QWord => new DataCell(long.MaxMagnitude(x.QWord, y.QWord)),
            CellTypes.Single => new DataCell(float.MaxMagnitudeNumber(x.Single, y.Single)),
            CellTypes.Double => new DataCell(double.MaxMagnitudeNumber(x.Double, y.Double)),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };
    }

    public static DataCell MinMagnitude(DataCell x, DataCell y)
    {
        if (x.CellType != y.CellType) throw new MismatchedDataTypeException();
        return x.CellType switch
        {
            CellTypes.DWord => new DataCell(int.MinMagnitude(x.DWord, y.DWord)),
            CellTypes.QWord => new DataCell(long.MinMagnitude(x.QWord, y.QWord)),
            CellTypes.Single => new DataCell(float.MinMagnitude(x.Single, y.Single)),
            CellTypes.Double => new DataCell(double.MinMagnitude(x.Double, y.Double)),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };
    }

    public static DataCell MinMagnitudeNumber(DataCell x, DataCell y)
    {
        if (x.CellType != y.CellType) throw new MismatchedDataTypeException();
        return x.CellType switch
        {
            CellTypes.DWord => new DataCell(int.MinMagnitude(x.DWord, y.DWord)),
            CellTypes.QWord => new DataCell(long.MinMagnitude(x.QWord, y.QWord)),
            CellTypes.Single => new DataCell(float.MinMagnitudeNumber(x.Single, y.Single)),
            CellTypes.Double => new DataCell(double.MinMagnitudeNumber(x.Double, y.Double)),
            CellTypes.Text => throw new NotSupportedException(),
            CellTypes.Bytes => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };
    }

    public static DataCell Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
    {
        return Parse(s, provider);
    }

    public static DataCell Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        return Parse(s, provider);
    }

    public static bool TryCreate<TOther>(TOther value, out DataCell result) where TOther : notnull
    {
        switch (Type.GetTypeCode(typeof(TOther)))
        {
            case TypeCode.Int32:
                result = new((int)(object)value);
                return true;
            case TypeCode.Int64:
                result = new((long)(object)value);
                return true;
            case TypeCode.Single:
                result = new((float)(object)value);
                return true;
            case TypeCode.Double:
                result = new((double)(object)value);
                return true;
            case TypeCode.String:
                result = new((string)(object)value);
                return true;
            default:
                if (typeof(TOther) == typeof(byte[]))
                {
                    result = new((byte[])(object)value);
                    return true;
                }
                result = new();
                return false;
        }
    }
    
    public static bool TryConvertFromChecked<TOther>(TOther value, out DataCell result) where TOther : INumberBase<TOther>
    {
        return TryCreate(value, out result);
    }

    public static bool TryConvertFromSaturating<TOther>(TOther value, out DataCell result) where TOther : INumberBase<TOther>
    {
        return TryConvertFromChecked(value, out result);
    }

    public static bool TryConvertFromTruncating<TOther>(TOther value, out DataCell result) where TOther : INumberBase<TOther>
    {
        return TryConvertFromChecked(value, out result);
    }

    public static bool TryConvertToChecked<TOther>(DataCell value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        switch (Type.GetTypeCode(typeof(TOther)))
        {
            case TypeCode.Int32:
                result = (TOther)(object)value.DWord;
                return true;
            case TypeCode.Int64:
                result = (TOther)(object)value.QWord;
                return true;
            case TypeCode.Single:
                result = (TOther)(object)value.Single;
                return true;
            case TypeCode.Double:
                result = (TOther)(object)value.Double;
                return true;
            case TypeCode.String:
                result = (TOther)(object)value.ExtractText().ToString();
                return true;
            default:
                if (typeof(TOther) == typeof(byte[]))
                {
                    result = (TOther)(object)value.ExtractBytes().ToArray();
                    return true;
                }
                result = default;
                return false;
        }
    }

    public static bool TryConvertToSaturating<TOther>(DataCell value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        return TryConvertToChecked(value, out result);
    }

    public static bool TryConvertToTruncating<TOther>(DataCell value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        return TryConvertToChecked(value, out result);
    }

    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out DataCell result)
    {
        return TryParse(s, provider, out result);
    }

    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out DataCell result)
    {
        return TryParse(s, provider, out result);
    }

    public static DataCell One => new(1.0);
    public static int Radix => 2;
    public static DataCell Zero => new(0.0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        switch (CellType)
        {
            case CellTypes.Text:
            {
                var buffer = (char[])Pointer;
                ArrayPool<char>.Shared.Return(buffer);
                break;
            }
            case CellTypes.Bytes:
            {
                var buffer = (byte[])Pointer;
                ArrayPool<byte>.Shared.Return(buffer);
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ExtractBytes() => new((byte[])Pointer, 0, DWord);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringRef ExtractText() => new(new((char[])Pointer, 0, DWord));

    public byte[] GetBytes() => ExtractBytes().ToArray();
    public string GetString() => new(ExtractText());
}