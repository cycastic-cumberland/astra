using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Astra.Common.Data;
using Astra.Common.Serializable;
using Astra.Common.StreamUtils;
using Astra.Engine.Data.Attributes;
using Astra.Engine.v2.Data;
using Microsoft.Extensions.Logging;

namespace Astra.Engine.Data;

public struct RegistrySettings
{
    public int BinaryTreeDegree { get; set; }
    public IndexerType? DefaultIndexerType { get; set; }
}

public class DataRegistry<T, TRegistry> : IDisposable, IQueryable<T>, IQueryProvider
    where TRegistry : IRegistry<TRegistry>
{
    private readonly TRegistry _registry;

    internal TRegistry InternalRegistry => _registry;

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
    
    public DataRegistry(RegistrySettings settings = default, ILoggerFactory? loggerFactory = null)
    {
        DynamicSerializable.EnsureBuilt<T>();
        _registry = TRegistry.Fabricate(new()
        {
            Columns = GetSchema(settings.DefaultIndexerType ?? IndexerType.Generic),
            BinaryTreeDegree = settings.BinaryTreeDegree
        }, loggerFactory);
        Query = new(this);
    }

    public void Dispose()
    {
        _registry.Dispose();
    }

    public int Count => _registry.RowsCount;
    
    public int Delete(RegistryQuery<T, TRegistry> query)
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

    private RegistryQuery<T, TRegistry> Query { get; }

    public RegistryQuery<T, TRegistry> CreateQuery(Expression expression) => new(this, expression);
    
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        return CreateQuery(expression);
    }

    IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(T)) throw new NotSupportedException(typeof(TElement).Name);
        return (IQueryable<TElement>)CreateQuery(expression);
    }

    public IEnumerable<T> Aggregate(ReadOnlyMemory<byte> predicate)
    {
        return _registry.Aggregate<T>(predicate);
    }

    public RegistryQuery<T, TRegistry> Where(Expression<Func<T, bool>> predicate)
    {
        return CreateQuery(Expression.Call(null,
            new Func<IQueryable<T>, Expression<Func<T, bool>>, IQueryable<T>>(
                Queryable.Where).Method, Expression,
            Expression.Quote(predicate)));
    }

    public RegistryQuery<T, TRegistry> Execute(Expression expression)
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

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _registry.GetEnumerator<T>();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Query).GetEnumerator();
    }

    public Type ElementType => Query.ElementType;

    public Expression Expression => Query.Expression;

    public IQueryProvider Provider => Query.Provider;
}

public class DataRegistry<T> : DataRegistry<T, DataRegistry>
{
    public DataRegistry(RegistrySettings settings = default, ILoggerFactory? loggerFactory = null)
        : base(settings, loggerFactory)
    {
        
    }

    public new DataRegistry.DynamicBufferBasedAggregationEnumerable<T> Aggregate(ReadOnlyMemory<byte> predicate)
    {
        return InternalRegistry.Aggregate<T>(predicate);
    }

    public DataRegistry.Enumerator<T> GetEnumerator()
    {
        return InternalRegistry.GetEnumerator<T>();
    }
}

public class ShinDataRegistry<T> : DataRegistry<T, ShinDataRegistry>
{
    public ShinDataRegistry(RegistrySettings settings = default, ILoggerFactory? loggerFactory = null)
        : base(settings, loggerFactory)
    {
        
    }

    public new ShinDataRegistry.WrappedEnumerable<T> Aggregate(ReadOnlyMemory<byte> predicate)
    {
        return InternalRegistry.Aggregate<T>(predicate);
    }

    public ShinDataRegistry.Enumerator<T> GetEnumerator()
    {
        return InternalRegistry.GetEnumerator<T>();
    }
}