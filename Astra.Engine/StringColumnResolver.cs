using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Astra.Common;

namespace Astra.Engine;

public sealed class StringColumnResolver(int offset, bool shouldBeHashed) : IColumnResolver, IDestructibleColumnResolver, IColumnResolver<string>
{
    private readonly AutoSerial<string> _serial = new();
    public DataType Type => DataType.Single;
    public int Occupying => sizeof(ulong);
    public int HashSize => Hash128.Size;
    public int Offset => offset;

    public void Initialize<T>(T row) where T : struct, IDataRow
    {
        EnrollId(_serial.Save(""), row);
    }
    
    public void Initialize<T>(Stream reader, Stream hashStream, T row) where T : struct, IDataRow
    {
        var size = reader.ReadInt();
        using var strBytes = BytesCluster.Rent(size);
        reader.ReadExactly(strBytes.Writer);
        EnrollId(_serial.Save(Encoding.UTF8.GetString(strBytes.Reader)), row);
        if (shouldBeHashed)
        {
            hashStream.WriteValue(Hash128.HashXx128(strBytes.Reader));
        }
    }
    
    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        // A bit different from Serialize, as in no length is written
        if (shouldBeHashed)
            writer.WriteValue(Hash128.HashXx128(MemoryMarshal.AsBytes(Dump(row).AsSpan())));
    }

    public void Destroy<T>(T row) where T : struct, IImmutableDataRow
    {
        var id = DumpId(row);
        _serial.Remove(id, out _);
    }

    public void Clear()
    {
        _serial.Clear();
    }

    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        writer.WriteValue(Dump(row));
    }

    public void Deserialize<T>(Stream reader, T row) where T : struct, IDataRow
    {
        var str = reader.ReadString();
        Enroll(str, row);
    }

    public Task SerializeAsync<T>(Stream writer, T row) where T : struct, IImmutableDataRow
    {
        throw new NotImplementedException();
    }

    public Task DeserializeAsync<T>(Stream reader, T row) where T : struct, IDataRow
    {
        throw new NotImplementedException();
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

    public string Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        var id = DumpId(row);
        var str = _serial[id];
        return str;
    }

    public void Enroll<TR>(string value, TR row) where TR : struct, IDataRow
    {
        var id = DumpId(row);
        var newId = _serial.Exchange(id, value, out _);
        EnrollId(newId, row);
    }
}