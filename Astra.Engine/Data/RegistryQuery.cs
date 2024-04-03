using System.Collections;
using System.Linq.Expressions;
using Astra.Common.Data.Queryable;
using Astra.Common.StreamUtils;

namespace Astra.Engine.Data;

public class RegistryQuery<T> : IQueryable<T>
{
    public struct Enumerator(DataRegistry.DynamicStreamBasedAggregationEnumerator<T> enumerator, Stream stream) : IEnumerator<T>
    {
        public void Dispose()
        {
            enumerator.Dispose();
            stream.Dispose();
        }

        public bool MoveNext() => enumerator.MoveNext();

        public void Reset() => enumerator.Reset();

        public T Current => enumerator.Current;

        object IEnumerator.Current => Current!;
    }
    
    private readonly DataRegistry<T> _registry;

    internal RegistryQuery(DataRegistry<T> registry, Expression? expression = null)
    {
        _registry = registry;
        Expression = expression ?? Expression.Constant(this);
        Provider = registry;
    }

    internal void Serialize(Stream stream)
    {
        new SinglePassExpressionAnalyzer<T>(Expression, stream, typeof(RegistryQuery<T>)).Analyze();
    }
    
    public Enumerator GetEnumerator()
    {
        var stream = MemoryStreamPool.Allocate();
        try
        {
            Serialize(stream);
            stream.Position = 0;
            return new(new(_registry.InternalRegistry, stream), stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }
}