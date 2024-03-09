using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Hashes;
using Astra.Common.Protocols;
using Astra.Common.StreamUtils;
using Astra.Engine.Data;
using Astra.Engine.Indexers;

namespace Astra.Engine.Resolvers;

public sealed class BytesColumnResolver(string columnName, int offset, int index, bool shouldBeHashed) : 
    IColumnResolver<ComparableBytesMemory>
{
    public DataType Type => DataType.Bytes;
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
        var length = reader.ReadLong();
        var cluster = BytesCluster.Rent((int)(length + sizeof(long)));
        try
        {
            Span<byte> lengthSpan = stackalloc byte[sizeof(long)];
            length.ToSpan(lengthSpan);
            lengthSpan.CopyTo(cluster.Writer[..sizeof(long)]);
            reader.ReadExactly(cluster.Writer[sizeof(long)..]);
            row.SetPeripheral(index, cluster);
        }
        catch
        {
            cluster.Dispose();
            throw;
        }

        if (!shouldBeHashed) return;
        var hash = Hash128.HashXx128(cluster.Reader[sizeof(long)..]);
        hash.CopyTo(hashStream);
    }

    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        if (!shouldBeHashed) return;
        var memory = row.ReadPeripheral(index);
        var hash = Hash128.HashXx128(memory.Span[sizeof(long)..]);
        hash.CopyTo(writer);
    }

    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        var memory = row.ReadPeripheral(index);
        var size = BitConverter.ToInt64(memory.Span[..sizeof(long)]);
        writer.WriteValue(size);
        writer.Write(memory.Span[sizeof(long)..]);
    }

    public void Clear()
    {
        
    }

    public ComparableBytesMemory Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        var memory = row.ReadPeripheral(index);
        var size = BitConverter.ToInt64(memory.Span[..sizeof(long)]);
        return memory.Slice(sizeof(long), (int)size);
    }
}