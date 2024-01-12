using Astra.Common;

namespace Astra.Engine;

public sealed class IntegerColumnResolver(int offset, bool shouldBeHashed) : IColumnResolver, IColumnResolver<int>
{
    public DataType Type => DataType.DWord;
    public int Occupying => sizeof(int);
    public int HashSize => sizeof(int);
    public int Offset => offset;

    public void Initialize<T>(T row) where T : struct, IDataRow
    {
        Enroll(0, row);
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

    public int Dump<TR>(TR row) where TR : struct, IImmutableDataRow
    {
        return BitConverter.ToInt32(row.Read[offset..(offset + Occupying)]);
    }

    public void Enroll<TR>(int value, TR row) where TR : struct, IDataRow
    {
        BitConverter.GetBytes(value).CopyTo(row.Write[offset..(offset + Occupying)]);
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
        throw new NotImplementedException();
    }

    public Task DeserializeAsync<T>(Stream reader, T row) where T : struct, IDataRow
    {
        throw new NotImplementedException();
    }
}