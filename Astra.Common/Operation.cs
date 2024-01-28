namespace Astra.Common;

public static class Operation
{
    public const uint Equal = 1;
    public const uint NotEqual = 2;
    
    private const uint FetchFromLeft = 4;
    private const uint FetchToRight = 8;
    private const uint IncludeLeft = 16;
    private const uint IncludeRight = 32;
    
    public const uint ClosedBetween = FetchFromLeft | FetchToRight | IncludeLeft | IncludeRight;
    public const uint HalfClosedFromBetween = FetchFromLeft | FetchToRight | IncludeLeft;
    public const uint HalfClosedToBetween = FetchFromLeft | FetchToRight | IncludeRight;
    public const uint OpenBetween = FetchFromLeft | FetchToRight;
    public const uint GreaterThan = FetchFromLeft;
    public const uint GreaterOrEqualsTo = FetchFromLeft | IncludeLeft;
    public const uint LesserThan = FetchToRight;
    public const uint LesserOrEqualsTo = FetchToRight | IncludeRight;
}