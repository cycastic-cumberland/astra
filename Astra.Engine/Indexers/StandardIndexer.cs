using Astra.Common;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class IntegerIndexer(IntegerColumnResolver resolver) :
    SelfContainedIndexer<int, ComposableIntegerIndexer,
        ComposableStandardIndexer<int, IntegerColumnResolver, IntegerStreamResolver>,
        ComposableStandardIndexer<int, IntegerColumnResolver, IntegerStreamResolver>>(new(resolver));

public sealed class LongIndexer(LongColumnResolver resolver) :
    SelfContainedIndexer<long, ComposableLongIndexer,
        ComposableStandardIndexer<long, LongColumnResolver, LongStreamResolver>,
        ComposableStandardIndexer<long, LongColumnResolver, LongStreamResolver>>(new(resolver));

public sealed class SingleIndexer(SingleColumnResolver resolver) :
    SelfContainedIndexer<float, ComposableSingleIndexer,
        ComposableStandardIndexer<float, SingleColumnResolver, SingleStreamResolver>,
        ComposableStandardIndexer<float, SingleColumnResolver, SingleStreamResolver>>(new(resolver));

public sealed class DoubleIndexer(DoubleColumnResolver resolver) :
    SelfContainedIndexer<double, ComposableDoubleIndexer,
        ComposableStandardIndexer<double, DoubleColumnResolver, DoubleStreamResolver>,
        ComposableStandardIndexer<double, DoubleColumnResolver, DoubleStreamResolver>>(new(resolver));

public sealed class BytesIndexer(BytesColumnResolver resolver) :
    SelfContainedIndexer<ComparableBytesMemory, ComposableBytesIndexer,
        ComposableStandardIndexer<ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>,
        ComposableStandardIndexer<ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>>(new(resolver));

public sealed class StringIndexer(StringColumnResolver resolver) :
    SelfContainedIndexer<StringWrapper, ComposableStringIndexer,
        ComposableStandardIndexer<StringWrapper, StringColumnResolver, StringWrapperStreamResolver>,
        ComposableStandardIndexer<StringWrapper, StringColumnResolver, StringWrapperStreamResolver>>(new(resolver));
