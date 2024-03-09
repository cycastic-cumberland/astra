using Astra.Collections;
using Astra.Common;
using Astra.Common.Data;
using Astra.Common.Protocols;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Types;

public abstract class SequenceTypeHandler<TUnit, T, TColumnResolver, TResolverFactory, TGenericIndexer,
    TGenericIndexerFactory, TFuzzyIndexer, TFuzzyIndexerFactory>(uint type, string typeName) : ITypeHandler
    where TUnit : IEquatable<TUnit>
    where T : IReadOnlyList<TUnit>
    where TColumnResolver : IColumnResolver<T>
    where TResolverFactory : IPeripheralResolverFactory<TColumnResolver>
    where TGenericIndexer : IIndexer, IPointIndexer<T>
    where TGenericIndexerFactory : IIndexerFactory<T, TColumnResolver, TGenericIndexer>
    where TFuzzyIndexer : IIndexer, IPointIndexer<T>
    where TFuzzyIndexerFactory: IFuzzyIndexerFactory<TUnit, T, TColumnResolver, TFuzzyIndexer>
{
    public uint FeaturingType => type;
    public TypeHandlingResult Process(ColumnSchemaSpecifications column, RegistrySchemaSpecifications registry, int index, int offset)
    {
        var isHashed = column.ShouldBeHashed ?? column.Indexer.Type != IndexerType.None;
        var resolver = TResolverFactory.Create(column.Name, offset, index, isHashed);
        IIndexer? indexer = column.Indexer.Type switch
        {
            IndexerType.Generic => TGenericIndexerFactory.Create(resolver),
            IndexerType.Fuzzy => TFuzzyIndexerFactory.Create(resolver),
            IndexerType.None => null,
            _ => throw new NotSupportedException($"Indexer not supported: {column.Indexer}")
        };
        
        return new()
        {
            Type = resolver.Type,
            TypeName = typeName,
            Resolver = resolver,
            Indexer = indexer,
            NewOffset = offset + resolver.Occupying,
            IsHashed = isHashed
        };
    }
}

public readonly struct StringResolverFactory : IPeripheralResolverFactory<StringColumnResolver>
{
    public static StringColumnResolver Create(string columnName, int offset, int index, bool isHashed)
    {
        return new(columnName, offset, index, isHashed);
    }
}

public readonly struct BytesResolverFactory : IPeripheralResolverFactory<BytesColumnResolver>
{
    public static BytesColumnResolver Create(string columnName, int offset, int index, bool isHashed)
    {
        return new(columnName, offset, index, isHashed);
    }
}

public readonly struct StringIndexerFactory : IIndexerFactory<StringWrapper, StringColumnResolver, StringIndexer>
{
    public static StringIndexer Create(StringColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct FuzzyStringIndexerFactory : IFuzzyIndexerFactory<char, StringWrapper, StringColumnResolver, FuzzyStringIndexer>
{
    public static FuzzyStringIndexer Create(StringColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct BytesIndexerFactory : IIndexerFactory<ComparableBytesMemory, BytesColumnResolver, BytesIndexer>
{
    public static BytesIndexer Create(BytesColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct FuzzyBytesIndexerFactory : IFuzzyIndexerFactory<byte, ComparableBytesMemory, BytesColumnResolver, FuzzyBytesIndexer>
{
    public static FuzzyBytesIndexer Create(BytesColumnResolver resolver)
    {
        return new(resolver);
    }
}

public sealed class StringTypeHandler()
    : SequenceTypeHandler<char, StringWrapper, StringColumnResolver, StringResolverFactory, StringIndexer,
        StringIndexerFactory, FuzzyStringIndexer, FuzzyStringIndexerFactory>(Feature, nameof(DataType.String)),
        IDefault<StringTypeHandler>
{
    public const uint Feature = DataType.StringMask;
    public static StringTypeHandler Default { get; } = new();
}

public sealed class BytesTypeHandler()
    : SequenceTypeHandler<byte, ComparableBytesMemory, BytesColumnResolver, BytesResolverFactory, BytesIndexer,
        BytesIndexerFactory, FuzzyBytesIndexer, FuzzyBytesIndexerFactory>(Feature, nameof(DataType.Bytes)),
        IDefault<BytesTypeHandler>
{
    public const uint Feature = DataType.BytesMask;
    public static BytesTypeHandler Default { get; } = new();
}
