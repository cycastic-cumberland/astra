using Astra.Common;
using Astra.Engine.Data;

namespace Astra.Engine.Resolvers;

public interface IColumnResolver
{
    public DataType Type { get; }
    public int Occupying { get; }
    public int HashSize { get; }
    public int Offset { get; }
    public void Initialize<T>(T row) where T : struct, IDataRow;
    public void Initialize<T>(Stream reader, Stream hashStream, T row) where T : struct, IDataRow;
    public void BeginHash<T>(Stream writer, T row) where T : struct, IImmutableDataRow;
    public void Serialize<T>(Stream writer, T row) where T : struct, IImmutableDataRow;
    public void Deserialize<T>(Stream reader, T row) where T : struct, IDataRow;
    public Task SerializeAsync<T>(Stream writer, T row) where T : struct, IImmutableDataRow;
    public Task DeserializeAsync<T>(Stream reader, T row) where T : struct, IDataRow;
    public void Clear();
}

public interface IDestructibleColumnResolver : IColumnResolver
{
    public void Destroy<T>(T row) where T : struct, IImmutableDataRow;
}

public interface IColumnResolver<T> : IColumnResolver
{
    public T Dump<TR>(TR row) where TR : struct, IImmutableDataRow;
    public void Enroll<TR>(T value, TR row) where TR : struct, IDataRow;
}
