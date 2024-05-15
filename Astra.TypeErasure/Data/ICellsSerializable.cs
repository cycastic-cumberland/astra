namespace Astra.TypeErasure.Data;

public interface ICellsSerializable
{
    public void SerializeToCells(Span<DataCell> cells);
    public void DeserializeFromCells(ReadOnlySpan<DataCell> cells);
}