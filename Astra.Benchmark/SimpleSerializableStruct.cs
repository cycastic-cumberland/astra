using Astra.Common.Data;
using Astra.Common.StreamUtils;
using Astra.TypeErasure.Data;

namespace Astra.Benchmark;

public struct SimpleSerializableStruct : IStreamSerializable, ICellsSerializable
{
    public int Value1 { get; set; }
    public string Value2 { get; set; }
    public string Value3 { get; set; }
    
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper
    {
        writer.SaveValue(Value1);
        writer.SaveValue(Value2);
        writer.SaveValue(Value3);
    }

    public void DeserializeStream<TStream>(TStream reader) where TStream : IStreamWrapper
    {
        Value1 = reader.LoadInt();
        Value2 = reader.LoadString();
        Value3 = reader.LoadString();
    }

    public void SerializeToCells(Span<DataCell> cells)
    {
        cells[0] = new(Value1);
        cells[1] = new(Value2);
        cells[2] = new(Value3);
    }

    public void DeserializeFromCells(ReadOnlySpan<DataCell> cells)
    {
        Value1 = cells[0].DWord;
        Value2 = cells[1].GetString();
        Value3 = cells[2].GetString();
    }
}