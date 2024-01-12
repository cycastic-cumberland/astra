namespace Astra.Common;

// Such dumbass names...
public static class PredicateType
{
    // -------------- LAYOUT --------------
    // [mask[predicate---][predicate---]]
    //  4    >= 4          >=4
    public const uint BinaryAndMask = 1;
    // [mask[predicate---][predicate---]]
    //  4    >= 4          >=4
    public const uint BinaryOrMask = 2;
    // [mask[offset[operation[data_type[data---]]]]]
    // 4     4      4         4         >= 1
    public const uint UnaryMask = 3;
}