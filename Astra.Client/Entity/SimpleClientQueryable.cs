using System.Linq.Expressions;
using Astra.Client.Aggregator;

namespace Astra.Client.Entity;

public readonly struct SimpleClientQueryable<T> : IQueryProvider
{
    private readonly AstraClient _client;
    internal AstraClient Client => _client;

    internal SimpleClientQueryable(AstraClient client) => _client = client;


    internal ValueTask<DynamicResultsSet<T>> RunQuery(ReadOnlyMemory<byte> predicates, CancellationToken cancellationToken)
    {
        return _client.AggregateAsync<T>(new GenericAstraQueryBranch(predicates), cancellationToken);
    }

    public Query<T> CreateQuery(Expression expression) => new(this, expression);
    
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        return CreateQuery(expression);
    }

    IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(T)) throw new NotSupportedException(typeof(TElement).Name);
        return (IQueryable<TElement>)CreateQuery(expression);
    }
    
    public Query<T> Execute(Expression expression)
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

    public Query<T> Where(Expression<Func<T, bool>> predicate)
    {
        return CreateQuery(Expression.Call(null,
            new Func<IQueryable<T>, Expression<Func<T, bool>>, IQueryable<T>>(
                Queryable.Where).Method, new Query<T>(this).Expression,
            Expression.Quote(predicate)));
    }
}