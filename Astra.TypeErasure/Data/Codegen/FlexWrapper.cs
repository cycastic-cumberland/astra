namespace Astra.TypeErasure.Data.Codegen;

public struct FlexWrapper<T>: ICellsSerializable
{
    public T Target { get; set; }
    
    public void SerializeToCells(Span<DataCell> cells)
    {
        FlexCompiler.GetDefaultSerializer<T>().SerializeToCells(cells, Target);
    }

    public void DeserializeFromCells(ReadOnlySpan<DataCell> cells)
    {
        Target = FlexCompiler.GetDefaultSerializer<T>().DeserializeFromCells(cells);
    }
}