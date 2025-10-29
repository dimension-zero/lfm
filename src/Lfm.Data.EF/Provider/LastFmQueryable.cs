using System.Collections;
using System.Linq.Expressions;

namespace Lfm.Data.EF.Provider;

/// <summary>
/// Queryable wrapper that uses LastFmQueryProvider for LINQ expression execution.
/// Implements IQueryable&lt;T&gt; to enable LINQ query syntax over Last.fm entities.
///
/// Usage:
/// <code>
/// var queryable = new LastFmQueryable&lt;Artist&gt;(provider);
/// var filtered = queryable.Where(a => a.User == "smarshal");
/// var ordered = filtered.OrderByDescending(a => a.PlayCount);
/// var limited = ordered.Take(10);
/// var results = await limited.ToListAsync();
/// </code>
/// </summary>
/// <typeparam name="T">Entity type (Artist, Track, Album, RecentTrack)</typeparam>
public class LastFmQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    /// <summary>
    /// Create a root queryable (no expression).
    /// </summary>
    public LastFmQueryable(LastFmQueryProvider provider)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = Expression.Constant(this);
    }

    /// <summary>
    /// Create a queryable with an expression (for chained LINQ operations).
    /// </summary>
    public LastFmQueryable(LastFmQueryProvider provider, Expression expression)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <summary>
    /// Element type (IQueryable interface).
    /// </summary>
    public Type ElementType => typeof(T);

    /// <summary>
    /// Expression tree representing the query (IQueryable interface).
    /// </summary>
    public Expression Expression { get; }

    /// <summary>
    /// Query provider that executes the expression (IQueryable interface).
    /// </summary>
    public IQueryProvider Provider { get; }

    /// <summary>
    /// Get enumerator for synchronous enumeration (IEnumerable interface).
    /// Note: Prefer ToListAsync() for async operations.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        // Synchronous enumeration - executes query immediately
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    /// <summary>
    /// Get enumerator for synchronous enumeration (IEnumerable interface).
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Get async enumerator (IAsyncEnumerable interface).
    /// Enables await foreach loops.
    /// </summary>
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        // Cast provider to IAsyncQueryProvider and execute query
        // ExecuteAsync<TResult> returns TResult, so we ask for Task<IEnumerable<T>>
        var asyncProvider = (LastFmQueryProvider)Provider;
        var task = asyncProvider.ExecuteAsync<Task<IEnumerable<T>>>(Expression, cancellationToken);
        return new AsyncEnumeratorWrapper<T>(task, cancellationToken);
    }

    /// <summary>
    /// Helper class to wrap Task&lt;IEnumerable&lt;T&gt;&gt; as IAsyncEnumerator&lt;T&gt;.
    /// </summary>
    private class AsyncEnumeratorWrapper<TElement> : IAsyncEnumerator<TElement>
    {
        private readonly Task<IEnumerable<TElement>> _task;
        private readonly CancellationToken _cancellationToken;
        private IEnumerator<TElement>? _enumerator;

        public AsyncEnumeratorWrapper(Task<IEnumerable<TElement>> task, CancellationToken cancellationToken)
        {
            _task = task;
            _cancellationToken = cancellationToken;
        }

        public TElement Current => _enumerator!.Current;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_enumerator == null)
            {
                var result = await _task.ConfigureAwait(false);
                _enumerator = result.GetEnumerator();
            }

            return _enumerator.MoveNext();
        }

        public ValueTask DisposeAsync()
        {
            _enumerator?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
