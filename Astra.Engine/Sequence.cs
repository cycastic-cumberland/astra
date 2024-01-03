namespace Astra.Engine;

public struct Sequence(uint start = 0)
{
    public uint Next => Interlocked.Increment(ref start);
    public uint Current => start;

    public uint Exchange(uint contender) => Interlocked.CompareExchange(ref start, start, contender);
}

public struct LongSequence(ulong start = 0)
{
    public ulong Next => Interlocked.Increment(ref start);
    public ulong Current => start;
    public ulong Exchange(ulong contender) => Interlocked.CompareExchange(ref start, start, contender);
}