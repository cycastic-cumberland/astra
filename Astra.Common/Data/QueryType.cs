namespace Astra.Common.Data;

// Such dumbass names...
public static class QueryType
{
    public const uint EndOfQuery = 0;
    // -------------- LAYOUT --------------
    // [mask[predicate---][predicate---]]
    //  4    >= 4          >=4
    public const uint IntersectMask = 1;
    // [mask[predicate---][predicate---]]
    //  4    >= 4          >=4
    public const uint UnionMask = 2;
    // [mask[offset[operation[data_type[data---]]]]]
    // 4     4      4         4         >= 1
    public const uint FilterMask = 3;
}