using System.Text;
using Astra.Common;
using Astra.Engine.Data;

namespace Astra.Engine.Resolvers;

public sealed class StringColumnResolver(string columnName, int offset, int index, bool shouldBeHashed) 
    : IColumnResolver<StringWrapper>
{
    private const int HeaderSize = sizeof(long);
    private const int SizeOffsetEnd = sizeof(int);
    public DataType Type => DataType.String;
    public int Occupying => sizeof(ulong);
    public int HashSize => Hash128.Size;
    public int Offset => offset;
    public string ColumnName => columnName;

    public void Initialize<T>(T row) where T : struct, IDataRow
    {
        var cluster = BytesCluster.Rent(Hash128.Size);
        try
        {
            row.SetPeripheral(index, cluster);
        }
        catch
        {
            cluster.Dispose();
            throw;
        }
    }
    
    public void Initialize<T>(Stream reader, Stream hashStream, T row) where T : struct, IDataRow
    {
        var length = reader.ReadInt();
        var cluster = BytesCluster.Rent(length + HeaderSize);
        try
        {
            reader.ReadExactly(cluster.Writer[HeaderSize..(HeaderSize + length)]);
            Span<byte> lengthSpan = stackalloc byte[sizeof(int)];
            length.ToSpan(lengthSpan);
            lengthSpan.CopyTo(cluster.Writer[..SizeOffsetEnd]);
            row.SetPeripheral(index, cluster);
        }
        catch
        {
            cluster.Dispose();
            throw;
        }

        if (!shouldBeHashed) return;
        var hash = Hash128.HashXx128(cluster.Reader[HeaderSize..(HeaderSize + length)]);
        hash.CopyTo(hashStream);
    }
    
    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        if (!shouldBeHashed) return;
        var memory = row.ReadPeripheral(index);
        var size = BitConverter.ToInt32(memory.Span[..SizeOffsetEnd]);
        var hash = Hash128.HashXx128(memory.Span[HeaderSize..(HeaderSize + size)]);
        hash.CopyTo(writer);
    }

    public void Clear()
    {
        
    }

    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        var memory = row.ReadPeripheral(index);
        var size = BitConverter.ToInt32(memory.Span[..SizeOffsetEnd]);
        writer.WriteValue(size);
        writer.Write(memory.Span[HeaderSize..(HeaderSize + size)]);
    }
    
    public StringWrapper Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        var memory = row.ReadPeripheral(index);
        var size = BitConverter.ToInt32(memory.Span[..SizeOffsetEnd]);
        var slice = memory.Slice(HeaderSize, size);
        return Encoding.UTF8.GetString(slice.Span);
    }
}