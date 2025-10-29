using System.Linq.Expressions;
using Lfm.Shared.Services;
using Microsoft.EntityFrameworkCore.Query;

namespace Lfm.Data.EF.Provider;

/// <summary>
/// Query provider that translates LINQ expressions to music data provider calls.
/// Supports multiple data sources (Last.fm API, Spotify files, YouTube files, etc.)
/// Implements both IQueryProvider (sync) and IAsyncQueryProvider (async) for EF Core compatibility.
///
/// Usage:
/// <code>
/// var provider = new LastFmQueryProvider(musicDataProvider, defaultUser);
/// var queryable = new LastFmQueryable&lt;Artist&gt;(provider);
/// var results = await queryable.Where(a => a.User == "smarshal").ToListAsync();
/// </code>
/// </summary>
public class LastFmQueryProvider : IAsyncQueryProvider
{
    private readonly IMusicDataProvider _dataProvider;
    private readonly string _defaultUser;

    /// <summary>
    /// Create a new query provider.
    /// </summary>
    /// <param name="dataProvider">Music data provider (Last.fm API, local files, etc.)</param>
    /// <param name="defaultUser">Default username/file path when not specified in LINQ query</param>
    public LastFmQueryProvider(IMusicDataProvider dataProvider, string defaultUser)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _defaultUser = defaultUser ?? throw new ArgumentNullException(nameof(defaultUser));
    }

    /// <summary>
    /// Create a queryable from an expression (IQueryProvider interface).
    /// </summary>
    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetGenericArguments()[0];

        try
        {
            var queryableType = typeof(LastFmQueryable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }

    /// <summary>
    /// Create a typed queryable from an expression (IQueryProvider interface).
    /// </summary>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new LastFmQueryable<TElement>(this, expression);
    }

    /// <summary>
    /// Execute a query synchronously (IQueryProvider interface).
    /// Note: Prefer ExecuteAsync for async operations.
    /// </summary>
    public object? Execute(Expression expression)
    {
        // Synchronous execution - not supported yet
        throw new NotSupportedException(
            "Synchronous execution not supported. Use ExecuteAsync or ToListAsync() instead.");
    }

    /// <summary>
    /// Execute a typed query synchronously (IQueryProvider interface).
    /// Note: Prefer ExecuteAsync for async operations.
    /// </summary>
    public TResult Execute<TResult>(Expression expression)
    {
        // Synchronous execution - not supported yet
        throw new NotSupportedException(
            "Synchronous execution not supported. Use ExecuteAsync or ToListAsync() instead.");
    }

    /// <summary>
    /// Execute a query asynchronously (IAsyncQueryProvider interface).
    /// This is the main execution path - handles LINQ expression translation and API calls.
    /// </summary>
    public TResult ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken = default)
    {
        // Determine entity type from expression
        var entityType = GetEntityType(expression);
        System.Diagnostics.Debug.WriteLine($"[ExecuteAsync] TResult={typeof(TResult).Name}, EntityType={entityType.Name}");

        // Create visitor and analyze expression tree
        var visitor = new LastFmExpressionVisitor(entityType);
        var descriptor = visitor.Analyze(expression);
        System.Diagnostics.Debug.WriteLine($"[ExecuteAsync] QueryType={descriptor.QueryType}, User={descriptor.User}, Limit={descriptor.Limit}");

        // Translate descriptor to API call info
        var translator = new QueryTranslator(_defaultUser);
        var apiCallInfo = translator.Translate(descriptor);

        // Execute query based on result type - avoid reflection, use direct dispatch
        return ExecuteAsyncInternal<TResult>(apiCallInfo, descriptor, entityType, cancellationToken);
    }

    /// <summary>
    /// Internal execution dispatcher - handles type matching without reflection.
    /// </summary>
    private TResult ExecuteAsyncInternal<TResult>(
        ApiCallInfo apiCallInfo,
        QueryDescriptor descriptor,
        Type entityType,
        CancellationToken cancellationToken)
    {
        var resultType = typeof(TResult);

        // Task<List<T>>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = resultType.GetGenericArguments()[0];

            // Task<List<T>> or Task<IEnumerable<T>>
            if (innerType.IsGenericType &&
                (innerType.GetGenericTypeDefinition() == typeof(List<>) ||
                 innerType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var elementType = innerType.GetGenericArguments()[0];
                return ExecuteListQuery<TResult>(apiCallInfo, descriptor, elementType, cancellationToken);
            }

            // Task<int> (Count)
            if (innerType == typeof(int))
            {
                var countTask = ExecuteCountQueryAsync(apiCallInfo, cancellationToken);
                return (TResult)(object)countTask;
            }

            // Task<long> (LongCount)
            if (innerType == typeof(long))
            {
                var countTask = ExecuteCountQueryAsync(apiCallInfo, cancellationToken);
                var longTask = countTask.ContinueWith(t => (long)t.Result, cancellationToken);
                return (TResult)(object)longTask;
            }

            // Task<T> (First/Single)
            return ExecuteScalarQuery<TResult>(apiCallInfo, descriptor, innerType, cancellationToken);
        }

        throw new NotSupportedException(
            $"Query execution result type '{resultType}' not supported. " +
            $"Use ToListAsync(), FirstAsync(), or CountAsync().");
    }

    /// <summary>
    /// Execute list query with proper type handling.
    /// Handles Task<List<T>> to Task<IEnumerable<T>> conversion.
    /// </summary>
    private TResult ExecuteListQuery<TResult>(
        ApiCallInfo apiCallInfo,
        QueryDescriptor descriptor,
        Type elementType,
        CancellationToken cancellationToken)
    {
        // Check if result type is Task<IEnumerable<T>> vs Task<List<T>>
        var resultType = typeof(TResult);
        var innerType = resultType.GetGenericArguments()[0];
        var needsEnumerableWrapper = innerType.IsGenericType &&
            innerType.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        System.Diagnostics.Debug.WriteLine($"[ExecuteListQuery] TResult={resultType.Name}, InnerType={innerType.Name}, NeedsWrapper={needsEnumerableWrapper}");

        // Use dynamic dispatch based on element type
        if (elementType == typeof(Entities.Artist))
        {
            var listTask = ExecuteListQueryAsync<Entities.Artist>(apiCallInfo, descriptor, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[ExecuteListQuery Artist] ListTask type={listTask.GetType().Name}");

            if (needsEnumerableWrapper)
            {
                // Wrap Task<List<Artist>> as Task<IEnumerable<Artist>>
                var enumerableTask = listTask.ContinueWith(t => (IEnumerable<Entities.Artist>)t.Result, cancellationToken);
                System.Diagnostics.Debug.WriteLine($"[ExecuteListQuery Artist] EnumerableTask type={enumerableTask.GetType().Name}, returning as TResult");
                return (TResult)(object)enumerableTask;
            }
            System.Diagnostics.Debug.WriteLine($"[ExecuteListQuery Artist] Returning ListTask directly as TResult");
            return (TResult)(object)listTask;
        }
        else if (elementType == typeof(Entities.Track))
        {
            var listTask = ExecuteListQueryAsync<Entities.Track>(apiCallInfo, descriptor, cancellationToken);

            if (needsEnumerableWrapper)
            {
                var enumerableTask = listTask.ContinueWith(t => (IEnumerable<Entities.Track>)t.Result, cancellationToken);
                return (TResult)(object)enumerableTask;
            }
            return (TResult)(object)listTask;
        }
        else if (elementType == typeof(Entities.Album))
        {
            var listTask = ExecuteListQueryAsync<Entities.Album>(apiCallInfo, descriptor, cancellationToken);

            if (needsEnumerableWrapper)
            {
                var enumerableTask = listTask.ContinueWith(t => (IEnumerable<Entities.Album>)t.Result, cancellationToken);
                return (TResult)(object)enumerableTask;
            }
            return (TResult)(object)listTask;
        }
        else if (elementType == typeof(Entities.RecentTrack))
        {
            var listTask = ExecuteListQueryAsync<Entities.RecentTrack>(apiCallInfo, descriptor, cancellationToken);

            if (needsEnumerableWrapper)
            {
                var enumerableTask = listTask.ContinueWith(t => (IEnumerable<Entities.RecentTrack>)t.Result, cancellationToken);
                return (TResult)(object)enumerableTask;
            }
            return (TResult)(object)listTask;
        }

        throw new NotSupportedException($"Entity type '{elementType}' not supported");
    }

    /// <summary>
    /// Execute scalar query with proper type handling.
    /// </summary>
    private TResult ExecuteScalarQuery<TResult>(
        ApiCallInfo apiCallInfo,
        QueryDescriptor descriptor,
        Type elementType,
        CancellationToken cancellationToken)
    {
        // Use dynamic dispatch based on element type
        if (elementType == typeof(Entities.Artist))
        {
            var task = ExecuteScalarQueryAsync<Entities.Artist>(apiCallInfo, descriptor, cancellationToken);
            return (TResult)(object)task;
        }
        else if (elementType == typeof(Entities.Track))
        {
            var task = ExecuteScalarQueryAsync<Entities.Track>(apiCallInfo, descriptor, cancellationToken);
            return (TResult)(object)task;
        }
        else if (elementType == typeof(Entities.Album))
        {
            var task = ExecuteScalarQueryAsync<Entities.Album>(apiCallInfo, descriptor, cancellationToken);
            return (TResult)(object)task;
        }
        else if (elementType == typeof(Entities.RecentTrack))
        {
            var task = ExecuteScalarQueryAsync<Entities.RecentTrack>(apiCallInfo, descriptor, cancellationToken);
            return (TResult)(object)task;
        }

        throw new NotSupportedException($"Entity type '{elementType}' not supported");
    }

    /// <summary>
    /// Execute a query that returns a list of entities.
    /// </summary>
    private async Task<List<T>> ExecuteListQueryAsync<T>(
        ApiCallInfo apiCallInfo,
        QueryDescriptor descriptor,
        CancellationToken cancellationToken) where T : class
    {
        System.Diagnostics.Debug.WriteLine($"[ExecuteListQueryAsync] QueryType={apiCallInfo.QueryType}, Artist={apiCallInfo.Artist}, User={apiCallInfo.User}, Limit={apiCallInfo.Limit}");

        var mapper = new ResultMapper();
        List<object> results;

        // Call appropriate data provider method based on query type
        switch (apiCallInfo.QueryType)
        {
            case QueryType.TopArtists:
                var artistsResult = await _dataProvider.GetTopArtistsAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    apiCallInfo.Limit,
                    apiCallInfo.Page);
                if (!artistsResult.IsSuccess)
                    throw new InvalidOperationException(artistsResult.ErrorMessage);
                results = mapper.MapArtists(artistsResult.Value!, apiCallInfo.User!)
                    .Cast<object>().ToList();
                break;

            case QueryType.TopTracks:
                var tracksResult = await _dataProvider.GetTopTracksAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    apiCallInfo.Limit,
                    apiCallInfo.Page);
                if (!tracksResult.IsSuccess)
                    throw new InvalidOperationException(tracksResult.ErrorMessage);
                results = mapper.MapTracks(tracksResult.Value!, apiCallInfo.User!)
                    .Cast<object>().ToList();
                break;

            case QueryType.ArtistTracks:
                var artistTracksResult = await _dataProvider.GetArtistTopTracksAsync(
                    apiCallInfo.Artist!,
                    apiCallInfo.Limit);
                if (!artistTracksResult.IsSuccess)
                    throw new InvalidOperationException(artistTracksResult.ErrorMessage);
                results = mapper.MapArtistTracks(artistTracksResult.Value!, apiCallInfo.Artist!, apiCallInfo.User ?? _defaultUser)
                    .Cast<object>().ToList();
                break;

            case QueryType.TopAlbums:
                var albumsResult = await _dataProvider.GetTopAlbumsAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    apiCallInfo.Limit,
                    apiCallInfo.Page);
                if (!albumsResult.IsSuccess)
                    throw new InvalidOperationException(albumsResult.ErrorMessage);
                results = mapper.MapAlbums(albumsResult.Value!, apiCallInfo.User!)
                    .Cast<object>().ToList();
                break;

            case QueryType.ArtistAlbums:
                var artistAlbumsResult = await _dataProvider.GetArtistTopAlbumsAsync(
                    apiCallInfo.Artist!,
                    apiCallInfo.Limit);
                if (!artistAlbumsResult.IsSuccess)
                    throw new InvalidOperationException(artistAlbumsResult.ErrorMessage);
                results = mapper.MapArtistAlbums(artistAlbumsResult.Value!, apiCallInfo.Artist!, apiCallInfo.User ?? _defaultUser)
                    .Cast<object>().ToList();
                break;

            case QueryType.RecentTracks:
                var recentTracksResult = await _dataProvider.GetRecentTracksAsync(
                    apiCallInfo.User!,
                    apiCallInfo.FromDate ?? DateTime.UtcNow.AddMonths(-1),
                    apiCallInfo.ToDate ?? DateTime.UtcNow,
                    apiCallInfo.Limit,
                    apiCallInfo.Page);
                if (!recentTracksResult.IsSuccess)
                    throw new InvalidOperationException(recentTracksResult.ErrorMessage);
                results = mapper.MapRecentTracks(recentTracksResult.Value!, apiCallInfo.User!)
                    .Cast<object>().ToList();
                break;

            default:
                throw new NotSupportedException($"Query type '{apiCallInfo.QueryType}' not supported");
        }

        // Apply client-side filters if any
        if (descriptor.ClientSideFilters.Any())
        {
            results = mapper.ApplyClientSideFilters(results, descriptor.ClientSideFilters);
        }

        // Handle Include() navigation properties
        if (apiCallInfo.IncludeNavigations.Any())
        {
            await PopulateNavigationPropertiesAsync(results, apiCallInfo, cancellationToken);
        }

        return results.Cast<T>().ToList();
    }

    /// <summary>
    /// Execute a query that returns a scalar value (First, Single, etc.).
    /// </summary>
    private async Task<T> ExecuteScalarQueryAsync<T>(
        ApiCallInfo apiCallInfo,
        QueryDescriptor descriptor,
        CancellationToken cancellationToken) where T : class
    {
        var results = await ExecuteListQueryAsync<T>(apiCallInfo, descriptor, cancellationToken);

        if (descriptor.IsFirstQuery)
        {
            return results.FirstOrDefault()
                ?? throw new InvalidOperationException("Sequence contains no elements");
        }

        if (descriptor.IsSingleQuery)
        {
            if (results.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");
            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element");
            return results[0];
        }

        return results.FirstOrDefault()!;
    }

    /// <summary>
    /// Execute a query that returns a count.
    /// </summary>
    private async Task<int> ExecuteCountQueryAsync(
        ApiCallInfo apiCallInfo,
        CancellationToken cancellationToken)
    {
        // For count queries, we need to fetch the first page to get total count
        // Data providers return total count in response attributes
        switch (apiCallInfo.QueryType)
        {
            case QueryType.TopArtists:
                var artistsResult = await _dataProvider.GetTopArtistsAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    1, // limit
                    1); // page
                if (!artistsResult.IsSuccess)
                    return 0;
                return int.TryParse(artistsResult.Value?.Attributes?.Total, out var artistCount)
                    ? artistCount : 0;

            case QueryType.TopTracks:
                var tracksResult = await _dataProvider.GetTopTracksAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    1,
                    1);
                if (!tracksResult.IsSuccess)
                    return 0;
                return int.TryParse(tracksResult.Value?.Attributes?.Total, out var trackCount)
                    ? trackCount : 0;

            case QueryType.TopAlbums:
                var albumsResult = await _dataProvider.GetTopAlbumsAsync(
                    apiCallInfo.User!,
                    apiCallInfo.Period!.Value,
                    1,
                    1);
                if (!albumsResult.IsSuccess)
                    return 0;
                return int.TryParse(albumsResult.Value?.Attributes?.Total, out var albumCount)
                    ? albumCount : 0;

            case QueryType.RecentTracks:
                var recentTracksResult = await _dataProvider.GetRecentTracksAsync(
                    apiCallInfo.User!,
                    apiCallInfo.FromDate ?? DateTime.UtcNow.AddMonths(-1),
                    apiCallInfo.ToDate ?? DateTime.UtcNow,
                    1,
                    1);
                if (!recentTracksResult.IsSuccess)
                    return 0;
                return int.TryParse(recentTracksResult.Value?.Attributes?.Total, out var recentCount)
                    ? recentCount : 0;

            default:
                // For artist-specific queries, we need to fetch all and count
                // This is inefficient but accurate
                var mapper = new ResultMapper();
                var results = await ExecuteListQueryAsync<object>(apiCallInfo, new QueryDescriptor(), cancellationToken);
                return results.Count;
        }
    }

    /// <summary>
    /// Populate navigation properties for Include() support.
    /// </summary>
    private async Task PopulateNavigationPropertiesAsync(
        List<object> results,
        ApiCallInfo apiCallInfo,
        CancellationToken cancellationToken)
    {
        var mapper = new ResultMapper();

        // Handle Track.Artist and Album.Artist includes
        if (apiCallInfo.IncludeNavigations.Contains("Artist"))
        {
            if (results.FirstOrDefault() is Entities.Track)
            {
                var tracks = results.Cast<Entities.Track>().ToList();
                await mapper.PopulateTrackNavigationsAsync(
                    tracks,
                    apiCallInfo.IncludeNavigations,
                    async artistName => await LookupArtistAsync(artistName, apiCallInfo.User ?? _defaultUser));
            }
            else if (results.FirstOrDefault() is Entities.Album)
            {
                var albums = results.Cast<Entities.Album>().ToList();
                await mapper.PopulateAlbumNavigationsAsync(
                    albums,
                    apiCallInfo.IncludeNavigations,
                    async artistName => await LookupArtistAsync(artistName, apiCallInfo.User ?? _defaultUser));
            }
        }
    }

    /// <summary>
    /// Lookup artist by name for navigation property population.
    /// </summary>
    private async Task<Entities.Artist?> LookupArtistAsync(string artistName, string user)
    {
        // Try to get artist from top artists (will use cache)
        var topArtistsResult = await _dataProvider.GetTopArtistsAsync(user, Shared.Configuration.LastFmPeriod.Overall, 1000, 1);
        if (!topArtistsResult.IsSuccess)
            return null;

        var apiArtist = topArtistsResult.Value?.Artists?.FirstOrDefault(a =>
            string.Equals(a.Name, artistName, StringComparison.OrdinalIgnoreCase));

        if (apiArtist == null)
            return null;

        var mapper = new ResultMapper();
        return mapper.MapArtists(new Shared.Models.TopArtists { Artists = new List<Shared.Models.Artist> { apiArtist } }, user)
            .FirstOrDefault();
    }

    /// <summary>
    /// Determine entity type from expression tree.
    /// </summary>
    private static Type GetEntityType(Expression expression)
    {
        // Start with the root expression type
        var current = expression;

        // Walk up the expression tree to find the entity type
        while (current != null)
        {
            var type = current.Type;

            // For IQueryable<T>, IOrderedQueryable<T>, IEnumerable<T> extract T
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IQueryable<>) ||
                    genericDef == typeof(IOrderedQueryable<>) ||
                    genericDef == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            // For constant expressions (root queryable), extract entity type
            if (current is ConstantExpression constant && constant.Value != null)
            {
                var valueType = constant.Value.GetType();

                // Check if it's LastFmQueryable<T>
                if (valueType.IsGenericType && valueType.Name.StartsWith("LastFmQueryable"))
                {
                    return valueType.GetGenericArguments()[0];
                }

                // Check interfaces
                var queryableInterface = valueType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                       (i.GetGenericTypeDefinition() == typeof(IQueryable<>) ||
                                        i.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>)));

                if (queryableInterface != null)
                {
                    return queryableInterface.GetGenericArguments()[0];
                }
            }

            // For method call expressions, check the source (argument[0])
            if (current is MethodCallExpression methodCall && methodCall.Arguments.Count > 0)
            {
                current = methodCall.Arguments[0];
                continue;
            }

            break;
        }

        return expression.Type;
    }
}
