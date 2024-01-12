using System.Runtime.CompilerServices;

namespace Astra.Common;

public class CommandNotSupported(string? msg = null) : Exception(msg);

public static class Command
{
#if DEBUG
    public const uint HelloWorld = 1U;
#endif
    public const uint UnsortedAggregate = 2U;
    public const uint UnsortedInsert = 3U;
    public const uint ConditionalDelete = 4U;
    public const uint CountAll = 5U;
    public const uint ConditionalCount = 6U;
    public const uint Clear = 7U;

    private const uint WriteModeMask = 1U << 31;
    private const uint CommandCountMask = ~WriteModeMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint commandCount, bool writeModeEnabled) SplitCommandHeader(uint raw)
        => (raw & CommandCountMask, (raw & WriteModeMask) != 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CreateReadHeader(uint count) => count & CommandCountMask;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CreateWriteHeader(uint count) => count | WriteModeMask;
}