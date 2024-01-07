namespace Astra.Engine;

public sealed class BytesColumnResolver(int offset, bool shouldBeHashed) : 
    IColumnResolver, IDestructibleColumnResolver, IColumnResolver<(BytesCluster cluster, Hash128 hash)>
{
    private readonly AutoSerial<(BytesCluster cluster, Hash128 hash)> _serial = new();
    public DataType Type => DataType.Bytes;
    public int Occupying => sizeof(ulong);
    public int HashSize => Hash128.Size;
    public int Offset => offset;
    
    public void Initialize<T>(T row) where T : struct, IDataRow
    {
        EnrollId(_serial.Save((BytesCluster.Empty, Hash128.Empty)), row);
    }

    public void Initialize<T>(Stream reader, Stream hashStream, T row) where T : struct, IDataRow
    {
        var array = reader.ReadCluster();
        var hash = Hash128.HashXx128(array.Reader);
        EnrollId(_serial.Save((array, hash)), row);
        if (shouldBeHashed)
        {
            hashStream.WriteValue(hash);
        }
    }

    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        // A bit different from Serialize, as in no length is written
        if (shouldBeHashed)
            writer.WriteValue(Dump(row).hash);
    }

    public void Destroy<T>(T row) where T : struct, IImmutableDataRow
    {
        var cluster = BytesCluster.Empty;
        try
        {
            var id = DumpId(row);
            _serial.Remove(id, out var oldItems);
            (cluster, _) = oldItems;
        }
        finally
        {
            cluster.Dispose();
        }
    }

    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        var bytes = Dump(row);
        writer.WriteValue(bytes.cluster);
    }

    public void Deserialize<T>(Stream reader, T row) where T : struct, IDataRow
    {
        var array = reader.ReadCluster();
        var hash = Hash128.HashXx128(array.Reader);
        Enroll((array, hash), row);
    }

    public Task SerializeAsync<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        throw new NotImplementedException();
    }

    public Task DeserializeAsync<T>(Stream reader, T row) where T : struct, IDataRow
    {
        throw new NotImplementedException();
    }

    public (BytesCluster cluster, Hash128 hash) Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        var id = DumpId(row);
        var bytes = _serial[id];
        return bytes;
    }

    public void Enroll<TR>((BytesCluster cluster, Hash128 hash) value, TR row) where TR : struct, IDataRow
    {
        var cluster = BytesCluster.Empty;
        try
        {
            var id = DumpId(row);
            var newId = _serial.Exchange(id, value, out var oldItems);
            (cluster, _) = oldItems;
            EnrollId(newId, row);
        }
        finally
        {
            cluster.Dispose();
        }
    }
    
    private ulong DumpId<T>(T row) where T : struct, IImmutableDataRow
    {
        return BitConverter.ToUInt64(row.Read[offset..(offset + Occupying)]);
    }

    private void EnrollId<T>(ulong id, T row) where T : struct, IDataRow
    {
        unsafe
        {
            new ReadOnlySpan<byte>(&id, sizeof(ulong)).CopyTo(row.Write[offset..(offset + Occupying)]);
        }
    }
}