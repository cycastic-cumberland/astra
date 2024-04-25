using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.Engine.Data.Attributes;
using Astra.Engine.Types;
using Microsoft.Extensions.Logging;

namespace Astra.Engine.Data;

public struct RegistrySettings
{
    public int BinaryTreeDegree { get; set; }
    public IndexerType? DefaultIndexerType { get; set; }
}

public class DataRegistry<T> : IDisposable, IQueryable<T>, IQueryProvider
{
    private readonly DataRegistry _registry;
    private readonly DataRegistry.Enumerable<T> _enumerable;

    internal DataRegistry InternalRegistry => _registry;

    private static ColumnSchemaSpecifications[] GetSchema(IndexerType defaultType)
    {
        return (from pi in TypeHelpers.ToAccessibleProperties<T>()
            let type = DataType.DotnetTypeToAstraType(pi.PropertyType)
            let indexerAttr = pi.GetCustomAttribute<IndexedAttribute>()
            select new ColumnSchemaSpecifications
            {
                Name = pi.Name,
                DataType = type,
                Indexer = indexerAttr?.Indexer ?? defaultType,
            }).ToArray();
    }
    
    public DataRegistry(RegistrySettings settings = default, ILoggerFactory? loggerFactory = null, IReadOnlyDictionary<uint, ITypeHandler>? handlers = null)
    {
        DynamicSerializable.EnsureBuilt<T>();
        _registry = new(new()
        {
            Columns = GetSchema(settings.DefaultIndexerType ?? IndexerType.Generic),
            BinaryTreeDegree = settings.BinaryTreeDegree
        }, loggerFactory, handlers);
        Query = new(this);
        _enumerable = _registry.AsEnumerable<T>();
    }

    public void Dispose()
    {
        _registry.Dispose();
    }

    public int Count => _registry.RowsCount;
    
    public int Delete(RegistryQuery<T> query)
    {
        using var localStream = LocalStreamWrapper.Create();
        query.Serialize(localStream.LocalStream);
        localStream.LocalStream.Position = 0;
        return _registry.Delete(localStream.LocalStream);
    }

    public bool Add(T value)
    {
        return _registry.Insert(value);
    }

    public int Add(IEnumerable<T> values)
    {
        return _registry.BulkInsert(values);
    }

    public int Clear() => _registry.Clear();

    private RegistryQuery<T> Query { get; }

    public RegistryQuery<T> CreateQuery(Expression expression) => new(this, expression);
    
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        return CreateQuery(expression);
    }

    IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(T)) throw new NotSupportedException(typeof(TElement).Name);
        return (IQueryable<TElement>)CreateQuery(expression);
    }

    public DataRegistry.DynamicBufferBasedAggregationEnumerable<T> Aggregate(ReadOnlyMemory<byte> predicate)
    {
        return _registry.Aggregate<T>(predicate);
    }

    public RegistryQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        return CreateQuery(Expression.Call(null,
            new Func<IQueryable<T>, Expression<Func<T, bool>>, IQueryable<T>>(
                Queryable.Where).Method, Expression,
            Expression.Quote(predicate)));
    }

    public RegistryQuery<T> Execute(Expression expression)
    {
        return CreateQuery(expression);
    }

    object IQueryProvider.Execute(Expression expression)
    {
        return Execute(expression);
    }

    TResult IQueryProvider.Execute<TResult>(Expression expression)
    {
        if (!typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult))) throw new NotSupportedException(typeof(TResult).Name);
        return (TResult)(object)Execute(expression);
    }

    public DataRegistry.Enumerator<T> GetEnumerator()
    {
        return _enumerable.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Query.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Query).GetEnumerator();
    }

    public Type ElementType => Query.ElementType;

    public Expression Expression => Query.Expression;

    public IQueryProvider Provider => Query.Provider;
}