using Astra.Common;
using Astra.Engine;

namespace Astra.Benchmark;

internal struct SimpleSerializableStruct : IAstraSerializable
{
    public int Value1 { get; set; }
    public string Value2 { get; set; }
    public string Value3 { get; set; }
        
    public void SerializeStream<TStream>(TStream writer) where TStream : Stream
    {
        writer.WriteValue(Value1);
        writer.WriteValue(Value2);
        writer.WriteValue(Value3);
    }

    public void DeserializeStream<TStream>(TStream reader) where TStream : Stream
    {
        Value1 = reader.ReadInt();
        Value2 = reader.ReadString();
        Value3 = reader.ReadString();
    }
}