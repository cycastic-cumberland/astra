namespace Astra.TypeErasure.Data.Codegen;

public interface IStatelessCellSerializable<T>
{
    public void SerializeToCells(Span<DataCell> cells, T data);
    public T DeserializeFromCells(ReadOnlySpan<DataCell> cells);
}