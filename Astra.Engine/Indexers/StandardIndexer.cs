using Astra.Common;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class BytesIndexer(BytesColumnResolver resolver) :
    SelfContainedIndexer<ComparableBytesMemory, ComposableBytesIndexer,
        ComposableStandardIndexer<ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>,
        ComposableStandardIndexer<ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>>(new(resolver));

public sealed class StringIndexer(StringColumnResolver resolver) :
    SelfContainedIndexer<StringWrapper, ComposableStringIndexer,
        ComposableStandardIndexer<StringWrapper, StringColumnResolver, StringWrapperStreamResolver>,
        ComposableStandardIndexer<StringWrapper, StringColumnResolver, StringWrapperStreamResolver>>(new(resolver));
