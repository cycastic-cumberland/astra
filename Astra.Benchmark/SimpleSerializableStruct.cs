using Astra.Common;
using Astra.Common.Data;
using Astra.Common.StreamUtils;
using Astra.Engine;

namespace Astra.Benchmark;

internal struct SimpleSerializableStruct : IAstraSerializable
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

    public void DeserializeStream<TStream>(TStream reader, ReadOnlySpan<string> columnSequence) where TStream : IStreamWrapper
    {
        Value1 = reader.LoadInt();
        Value2 = reader.LoadString();
        Value3 = reader.LoadString();
    }
}