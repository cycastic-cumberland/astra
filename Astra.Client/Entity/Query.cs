using System.Collections;
using System.Linq.Expressions;
using Astra.Client.Aggregator;
using Astra.Common.Data.Queryable;
using Astra.Common.StreamUtils;

namespace Astra.Client.Entity;

public interface IAsyncQueryable<out T> : IAsyncEnumerable<T>, IQueryable<T>;

public class Query<T> : IAsyncQueryable<T>
{
    public class AsyncEnumerator : IAsyncEnumerator<T>
    {
        private readonly Query<T> _host;
        private readonly CancellationToken _cancellationToken;
        private int _iterator = -1;
        private T _current = default!;

        public AsyncEnumerator(Query<T> host, CancellationToken cancellationToken)
        {
            _host = host;
            _cancellationToken = cancellationToken;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            var list = await _host.RunQueryAsync(_cancellationToken);
            if (++_iterator >= list.Count) return false;
            _current = list[_iterator];
            return true;
        }

        public T Current => _current;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly Query<T> _host;
        private List<T>.Enumerator _enumerator;
        private uint _stage;

        public Enumerator(Query<T> host) => _host = host;


        public void Dispose()
        {
            if (_stage > 0) 
                _enumerator.Dispose();
            _stage = 0;
        }

        public bool MoveNext()
        {
            switch (_stage)
            {
                case 0:
                {
                    _enumerator = _host.RunQuery().GetEnumerator();
                    _stage++;
                    goto default;
                }
                default:
                {
                    return _enumerator.MoveNext();
                }
            }
        }

        public void Reset()
        {
            Dispose();
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current!;
    }
    
    private readonly SimpleClientQueryable<T> _queryable;
    private List<T>? _processed;
    
    internal Query(SimpleClientQueryable<T> queryable, Expression? expression = null)
    {
        _queryable = queryable;
        Expression = expression ?? Expression.Constant(this);
    }
    
    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider => _queryable;

    private void Serialize(Stream stream)
    {
        new SinglePassExpressionAnalyzer<T>(Expression, stream, typeof(Query<T>)).Analyze();
    }

    internal async ValueTask<List<T>> RunQueryAsync(CancellationToken cancellationToken = default)
    {
        if (_processed != null) return _processed;
        if (Expression is ConstantExpression constantExpression && constantExpression.Value == this)
        {
            _processed = new();
            return _processed;
        }
        var stream = MemoryStreamPool.Allocate();
        try
        {
            Serialize(stream);
            stream.Position = 0;
            using var set = await _queryable.Client.AggregateAsync<T>(new GenericAstraQueryBranch(new(
                    stream.GetBuffer(),
                    0,
                    (int)stream.Length)),
                cancellationToken);
            _processed = new();
            foreach (var value in set)
            {
                _processed.Add(value);
            }

            return _processed;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private List<T> RunQuery()
    {
        if (_processed != null) return _processed;
        var task = Task.Run(async () => await RunQueryAsync());
        task.Wait();
        return task.Result;
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new(this, cancellationToken);
    }

    IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        return GetAsyncEnumerator(cancellationToken);
    }

    public Enumerator GetEnumerator() => new(this);
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Task<int> DeleteAsync(CancellationToken cancellationToken = default)
    {
        using var stream = LocalStreamWrapper.Create();
        Serialize(stream.LocalStream);
        stream.LocalStream.Position = 0;
        return _queryable.Client.ConditionalDeleteAsync(new GenericAstraQueryBranch(stream.LocalStream.GetBuffer()
                .AsMemory()[..(int)stream.LocalStream.Length]),
            cancellationToken);
    }
}