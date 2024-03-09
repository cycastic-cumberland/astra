using Astra.Common;
using Astra.Common.Data;
using Astra.Common.StreamUtils;

namespace Astra.Tests;

internal struct SimpleSerializableStruct : IAstraSerializable
{
    public int Value1 { get; set; }
    public string Value2 { get; set; }
    public string Value3 { get; set; }
    public byte[] Value4 { get; set; }
    public float Value5 { get; set; }
        
    public void SerializeStream<TStream>(TStream writer) where TStream : IStreamWrapper
    {
        writer.SaveValue(Value1);
        writer.SaveValue(Value2);
        writer.SaveValue(Value3);
        writer.SaveValue(Value4);
        writer.SaveValue(Value5);
    }

    public void DeserializeStream<TStream>(TStream reader, ReadOnlySpan<string> columnSequence) where TStream : IStreamWrapper
    {
        Value1 = reader.LoadInt();
        Value2 = reader.LoadString();
        Value3 = reader.LoadString();
        Value4 = reader.LoadBytes();
        Value5 = reader.LoadSingle();
    }
}