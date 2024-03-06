using Astra.Common;

namespace Astra.Client.Simple.Aggregator;

public interface IAstraQueryBranch
{
    public ReadOnlyMemory<byte> DumpMemory();
    public ReadOnlySpan<byte> Dump();
}

public readonly struct GenericAstraQueryBranch(byte[] bytes) : IAstraQueryBranch
{
    public ReadOnlyMemory<byte> DumpMemory()
    {
        return bytes;
    }
    
    public ReadOnlySpan<byte> Dump()
    {
        return bytes;
    }

    public GenericAstraQueryBranch And(GenericAstraQueryBranch other)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.BinaryAndMask);
        stream.Write(Dump());
        stream.Write(other.Dump());
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
    
    public GenericAstraQueryBranch Or(GenericAstraQueryBranch other)
    {
        using var stream = MemoryStreamPool.Allocate();
        stream.WriteValue(PredicateType.BinaryOrMask);
        stream.Write(Dump());
        stream.Write(other.Dump());
        return new(stream.GetBuffer()[..(int)stream.Length]);
    }
}