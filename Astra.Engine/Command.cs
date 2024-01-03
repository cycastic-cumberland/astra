namespace Astra.Engine;

public class CommandNotSupported(string? msg = null) : Exception(msg);

public static class Command
{
    public const uint HelloWorld = 1U;
    public const uint UnsortedAggregate = 2U;
    public const uint UnsortedInsert = 3U;
    public const uint Delete = 4U;
}