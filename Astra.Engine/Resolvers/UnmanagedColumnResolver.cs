using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Astra.Common;
using Astra.Engine.Data;

namespace Astra.Engine.Resolvers;

file static class ResolverHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyBytes<T>(this T value, Span<byte> toSpan) where T : unmanaged
    {
        unsafe
        {
            var fromSpan = new Span<byte>(&value, sizeof(T));
            fromSpan.CopyTo(toSpan);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ConvertBytes<T>(this ReadOnlySpan<byte> bytes) where T : unmanaged
    {
        return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(bytes));
    }
}

public abstract class UnmanagedColumnResolver<TD>(string columnName, int offset, bool shouldBeHashed, DataType type) 
    : IColumnResolver<TD>
    where TD : unmanaged
{
    public DataType Type => type;
    public int Occupying => Unsafe.SizeOf<TD>();
    public int HashSize => Unsafe.SizeOf<TD>();
    public int Offset => offset;
    public string ColumnName => columnName;

    public void Initialize<T>(T row) where T : struct, IDataRow
    {
        Enroll(new TD(), row);
    }

    public void Initialize<T>(Stream reader, Stream hashStream, T row) where T : struct, IDataRow
    {
        Deserialize(reader, row);
        if (shouldBeHashed) Serialize(hashStream, row);
    }

    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        if (shouldBeHashed) Serialize(writer, row);
    }

    public TD Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        return row.Read[offset..(offset + Occupying)].ConvertBytes<TD>();
    }

    public void Enroll<TR>(TD value, TR row) where TR : struct, IDataRow
    {
        value.CopyBytes(row.Write[offset..(offset + Occupying)]);
    }

    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        writer.Write(row.Read[offset..(offset + Occupying)]);
    }

    public void Deserialize<T>(Stream reader, T row) where T : struct, IDataRow
    {
        reader.ReadExactly(row.Write[offset..(offset + Occupying)]);
    }

    public Task SerializeAsync<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        throw new NotSupportedException();
    }

    public Task DeserializeAsync<T>(Stream reader, T row) where T : struct, IDataRow
    {
        throw new NotSupportedException();
    }

    public void Clear()
    {
        
    }
}

public sealed class IntegerColumnResolver(string columnName, int offset, bool shouldBeHashed) 
    : UnmanagedColumnResolver<int>(columnName, offset, shouldBeHashed, DataType.DWord);

public sealed class LongColumnResolver(string columnName, int offset, bool shouldBeHashed) 
    : UnmanagedColumnResolver<long>(columnName, offset, shouldBeHashed, DataType.QWord);

public sealed class SingleColumnResolver(string columnName, int offset, bool shouldBeHashed) 
    : UnmanagedColumnResolver<float>(columnName, offset, shouldBeHashed, DataType.Single);

public sealed class DoubleColumnResolver(string columnName, int offset, bool shouldBeHashed) 
    : UnmanagedColumnResolver<double>(columnName, offset, shouldBeHashed, DataType.Double);

public sealed class DecimalColumnResolver(string columnName, int offset, bool shouldBeHashed) 
    : UnmanagedColumnResolver<decimal>(columnName, offset, shouldBeHashed, DataType.Decimal);
