using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class IntegerRangeIndexer(IntegerColumnResolver resolver, int degree)
    : SelfContainedIndexer<int, ComposableIntegerRangeIndexer,
        ComposableNumericIndexer<int, UnmanagedColumnResolver<int>>,
        ComposableNumericIndexer<int, UnmanagedColumnResolver<int>>>(new(resolver, degree));

public sealed class LongRangeIndexer(LongColumnResolver resolver, int degree)
    : SelfContainedIndexer<long, ComposableLongRangeIndexer,
        ComposableNumericIndexer<long, UnmanagedColumnResolver<long>>,
        ComposableNumericIndexer<long, UnmanagedColumnResolver<long>>>(new(resolver, degree));

public sealed class SingleRangeIndexer(SingleColumnResolver resolver, int degree)
    : SelfContainedIndexer<float, ComposableSingleRangeIndexer,
        ComposableNumericIndexer<float, UnmanagedColumnResolver<float>>,
        ComposableNumericIndexer<float, UnmanagedColumnResolver<float>>>(new(resolver, degree));

public sealed class DoubleRangeIndexer(DoubleColumnResolver resolver, int degree)
    : SelfContainedIndexer<double, ComposableDoubleRangeIndexer,
        ComposableNumericIndexer<double, UnmanagedColumnResolver<double>>,
        ComposableNumericIndexer<double, UnmanagedColumnResolver<double>>>(new(resolver, degree));

public sealed class DecimalRangeIndexer(DecimalColumnResolver resolver, int degree)
    : SelfContainedIndexer<decimal, ComposableDecimalRangeIndexer,
        ComposableNumericIndexer<decimal, UnmanagedColumnResolver<decimal>>,
        ComposableNumericIndexer<decimal, UnmanagedColumnResolver<decimal>>>(new(resolver, degree));

