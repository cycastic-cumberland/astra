using Astra.Common;
using Astra.Common.Data;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Indexers;

public sealed class FuzzyStringIndexer(StringColumnResolver resolver)
    : SelfContainedIndexer<StringWrapper, ComposableFuzzyStringIndexer,
        ComposableFuzzyIndexer<char, StringWrapper, StringColumnResolver, StringWrapperStreamResolver>,
        ComposableFuzzyIndexer<char, StringWrapper, StringColumnResolver, StringWrapperStreamResolver>>(new(resolver));
        
public sealed class FuzzyBytesIndexer(BytesColumnResolver resolver)
    : SelfContainedIndexer<ComparableBytesMemory, ComposableFuzzyBytesIndexer,
        ComposableFuzzyIndexer<byte, ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>,
        ComposableFuzzyIndexer<byte, ComparableBytesMemory, BytesColumnResolver, BytesStreamResolver>>(new(resolver));