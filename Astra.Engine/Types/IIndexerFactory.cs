using System.Numerics;
using Astra.Engine.Indexers;
using Astra.Engine.Resolvers;

namespace Astra.Engine.Types;

public interface IIndexerFactory<T, in TR, out TI>
    where TR : IColumnResolver<T>
    where TI : IIndexer
{
    public static abstract TI Create(TR resolver);
}

public interface IRangeIndexerFactory<T, in TR, out TI> 
    where T : unmanaged, INumber<T>
    where TR : IColumnResolver<T>
    where TI : IIndexer
{
    public static abstract TI Create(TR resolver, int degree);
}

public interface IFuzzyIndexerFactory<TUnit, T, in TR, out TI>
    where TUnit : IEquatable<TUnit>
    where T : IReadOnlyList<TUnit>
    where TR : IColumnResolver<T>
    where TI : IIndexer
{
    public static abstract TI Create(TR resolver);
}
