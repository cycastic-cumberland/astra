using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class IntegerIndexer(IntegerColumnResolver resolver, int degree)
    : SelfContainedIndexer<int, ComposableIntegerIndexer,
        ComposableNumericIndexer<int, IntegerColumnResolver>,
        ComposableNumericIndexer<int, IntegerColumnResolver>>(new(resolver, degree));

public sealed class LongIndexer(LongColumnResolver resolver, int degree)
    : SelfContainedIndexer<long, ComposableLongIndexer,
        ComposableNumericIndexer<long, LongColumnResolver>,
        ComposableNumericIndexer<long, LongColumnResolver>>(new(resolver, degree));

public sealed class SingleIndexer(SingleColumnResolver resolver, int degree)
    : SelfContainedIndexer<float, ComposableSingleIndexer,
        ComposableNumericIndexer<float, SingleColumnResolver>,
        ComposableNumericIndexer<float, SingleColumnResolver>>(new(resolver, degree));

public sealed class DoubleIndexer(DoubleColumnResolver resolver, int degree)
    : SelfContainedIndexer<double, ComposableDoubleIndexer,
        ComposableNumericIndexer<double, DoubleColumnResolver>,
        ComposableNumericIndexer<double, DoubleColumnResolver>>(new(resolver, degree));
