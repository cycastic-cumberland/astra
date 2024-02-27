using System.Numerics;
using Astra.Collections;
using Astra.Collections.RangeDictionaries.BTree;
using Astra.Common;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Types;

file static class NumericTypeHandler
{
    public const int DefaultBinaryTreeDegree = 100;
}

public abstract class NumericTypeHandler<T, TColumnResolver, TResolverFactory, TGenericIndexer, TGenericIndexerFactory,
    TRangeIndexer, TRangeIndexerFactory>(uint type, string typeName) : ITypeHandler
    where T : unmanaged, INumber<T>
    where TColumnResolver : IColumnResolver<T>
    where TResolverFactory : IResolverFactory<TColumnResolver>
    where TGenericIndexer : IIndexer, IPointIndexer<T>
    where TGenericIndexerFactory : IIndexerFactory<T, TColumnResolver, TGenericIndexer>
    where TRangeIndexer : IIndexer, IPointIndexer<T>
    where TRangeIndexerFactory : IRangeIndexerFactory<T, TColumnResolver, TRangeIndexer>
{
    public uint FeaturingType => type;

    public TypeHandlingResult Process(ColumnSchemaSpecifications column, RegistrySchemaSpecifications registry, int index, int offset)
    {
        var degree = registry.BinaryTreeDegree < BTreeMap.MinDegree
            ? NumericTypeHandler.DefaultBinaryTreeDegree
            : registry.BinaryTreeDegree;
        var isHashed = column.ShouldBeHashed ?? column.Indexer != IndexerType.None;
        var resolver = TResolverFactory.Create(offset, isHashed);
        IIndexer? indexer = column.Indexer switch
        {
            IndexerType.Generic => TGenericIndexerFactory.Create(resolver),
            IndexerType.Range => TRangeIndexerFactory.Create(resolver, degree),
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

public readonly struct IntegerResolverFactory : IResolverFactory<IntegerColumnResolver>
{
    public static IntegerColumnResolver Create(int offset, bool isHashed)
    {
        return new(offset, isHashed);
    }
}

public readonly struct LongResolverFactory : IResolverFactory<LongColumnResolver>
{
    public static LongColumnResolver Create(int offset, bool isHashed)
    {
        return new(offset, isHashed);
    }
}

public readonly struct SingleResolverFactory : IResolverFactory<SingleColumnResolver>
{
    public static SingleColumnResolver Create(int offset, bool isHashed)
    {
        return new(offset, isHashed);
    }
}

public readonly struct DoubleResolverFactory : IResolverFactory<DoubleColumnResolver>
{
    public static DoubleColumnResolver Create(int offset, bool isHashed)
    {
        return new(offset, isHashed);
    }
}

public readonly struct IntegerIndexerFactory : IIndexerFactory<int, IntegerColumnResolver, IntegerIndexer>
{
    public static IntegerIndexer Create(IntegerColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct IntegerRangeIndexerFactory : IRangeIndexerFactory<int, IntegerColumnResolver, IntegerRangeIndexer>
{
    public static IntegerRangeIndexer Create(IntegerColumnResolver resolver, int degree)
    {
        return new(resolver, degree);
    }
}

public readonly struct LongIndexerFactory : IIndexerFactory<long, LongColumnResolver, LongIndexer>
{
    public static LongIndexer Create(LongColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct LongRangeIndexerFactory : IRangeIndexerFactory<long, LongColumnResolver, LongRangeIndexer>
{
    public static LongRangeIndexer Create(LongColumnResolver resolver, int degree)
    {
        return new(resolver, degree);
    }
}

public readonly struct SingleIndexerFactory : IIndexerFactory<float, SingleColumnResolver, SingleIndexer>
{
    public static SingleIndexer Create(SingleColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct SingleRangeIndexerFactory : IRangeIndexerFactory<float, SingleColumnResolver, SingleRangeIndexer>
{
    public static SingleRangeIndexer Create(SingleColumnResolver resolver, int degree)
    {
        return new(resolver, degree);
    }
}

public readonly struct DoubleIndexerFactory : IIndexerFactory<double, DoubleColumnResolver, DoubleIndexer>
{
    public static DoubleIndexer Create(DoubleColumnResolver resolver)
    {
        return new(resolver);
    }
}

public readonly struct DoubleRangeIndexerFactory : IRangeIndexerFactory<double, DoubleColumnResolver, DoubleRangeIndexer>
{
    public static DoubleRangeIndexer Create(DoubleColumnResolver resolver, int degree)
    {
        return new(resolver, degree);
    }
}

public sealed class IntegerTypeHandler()
    : NumericTypeHandler<int, IntegerColumnResolver, IntegerResolverFactory, IntegerIndexer, IntegerIndexerFactory, 
        IntegerRangeIndexer, IntegerRangeIndexerFactory>(Feature, nameof(DataType.DWord)),
        IDefault<IntegerTypeHandler>
{
    public const uint Feature = DataType.DWordMask;
    public static IntegerTypeHandler Default { get; } = new();
}

public sealed class LongTypeHandler() : NumericTypeHandler<long, LongColumnResolver, LongResolverFactory, 
    LongIndexer, LongIndexerFactory, LongRangeIndexer, LongRangeIndexerFactory>(Feature, nameof(DataType.QWord)),
    IDefault<LongTypeHandler>
{
    public const uint Feature = DataType.QWordMask;
    public static LongTypeHandler Default { get; } = new();
}

public sealed class SingleTypeHandler() : NumericTypeHandler<float, SingleColumnResolver, SingleResolverFactory, 
    SingleIndexer, SingleIndexerFactory, SingleRangeIndexer, SingleRangeIndexerFactory>(Feature, nameof(DataType.Single)),
    IDefault<SingleTypeHandler>
{
    public const uint Feature = DataType.SingleMask;
    public static SingleTypeHandler Default { get; } = new();
}

public sealed class DoubleTypeHandler() : NumericTypeHandler<double, DoubleColumnResolver, DoubleResolverFactory, 
    DoubleIndexer, DoubleIndexerFactory, DoubleRangeIndexer, DoubleRangeIndexerFactory>(Feature, nameof(DataType.Double)),
    IDefault<DoubleTypeHandler>
{
    public const uint Feature = DataType.DoubleMask;
    public static DoubleTypeHandler Default { get; } = new();
}
