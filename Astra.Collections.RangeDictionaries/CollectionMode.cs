namespace Astra.Collections.RangeDictionaries;

public static class CollectionFlags
{
    public const int Between = 0;
    public const int Inverted = 1 << 31;
    public const int ExcludeFrom = 1 << 0;
    public const int ExcludeTo = 1 << 1;
}

public enum CollectionMode
{
    ClosedInterval = CollectionFlags.Between,
    HalfClosedLeftInterval = CollectionFlags.Between | CollectionFlags.ExcludeTo,
    HalfClosedRightInterval = CollectionFlags.Between | CollectionFlags.ExcludeFrom,
    OpenInterval = CollectionFlags.Between | CollectionFlags.ExcludeFrom | CollectionFlags.ExcludeTo,
    
    UnboundedClosedInterval = CollectionFlags.Between | CollectionFlags.Inverted,
    UnboundedHalfClosedLeftInterval = CollectionFlags.Between | CollectionFlags.ExcludeTo | CollectionFlags.Inverted,
    UnboundedHalfClosedRightInterval = CollectionFlags.Between | CollectionFlags.ExcludeFrom | CollectionFlags.Inverted,
    UnboundedOpenInterval = CollectionFlags.Between | CollectionFlags.ExcludeFrom | CollectionFlags.ExcludeTo | CollectionFlags.Inverted,
}