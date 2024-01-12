namespace Astra.Common;

public static class CommonProtocol
{
    public const uint AstraCommonVersion = 0x000106U;
    public const int LongStringThreshold = 96;
    public const int ThreadLocalStreamDisposalThreshold = int.MaxValue / 4; // 512 MiB
}