using System.Linq.Expressions;
using Lfm.Data.EF.Entities;
using Lfm.Data.EF.Utilities;
using Lfm.Shared.Configuration;

namespace Lfm.Data.EF.Provider;

/// <summary>
/// Expression visitor that analyzes LINQ expression trees and extracts query parameters.
/// Walks the expression tree to determine query type, filters, sorting, paging, etc.
///
/// Example:
/// <code>
/// var expression = queryable
///     .Where(a => a.User == "smarshal")
///     .OrderByDescending(a => a.PlayCount)
///     .Take(10)
///     .Expression;
///
/// var visitor = new LastFmExpressionVisitor();
/// var descriptor = visitor.Analyze(expression);
/// // descriptor.QueryType == QueryType.TopArtists
/// // descriptor.User == "smarshal"
/// // descriptor.SortBy == "PlayCount", SortDirection == "Descending"
/// // descriptor.Limit == 10
/// </code>
/// </summary>
public class LastFmExpressionVisitor : ExpressionVisitor
{
    private readonly QueryDescriptor _descriptor = new();
    private readonly Type _entityType;

    /// <summary>
    /// Create a visitor for a specific entity type.
    /// </summary>
    public LastFmExpressionVisitor(Type entityType)
    {
        _entityType = entityType;
    }

    /// <summary>
    /// Analyze an expression tree and extract query parameters.
    /// </summary>
    public QueryDescriptor Analyze(Expression expression)
    {
        // Determine query type from entity type
        _descriptor.QueryType = DetermineQueryType(_entityType);

        // Visit the expression tree
        Visit(expression);

        return _descriptor;
    }

    /// <summary>
    /// Visit method call expressions (Where, OrderBy, Take, Skip, Include, etc.).
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Visit nested method calls first (right-to-left)
        Visit(node.Arguments[0]);

        switch (node.Method.Name)
        {
            case "Where":
                VisitWhere(node);
                break;

            case "OrderBy":
            case "OrderByDescending":
                VisitOrderBy(node);
                break;

            case "ThenBy":
            case "ThenByDescending":
                // Secondary sorting not supported by Last.fm API
                _descriptor.ClientSideFilters.Add($"ThenBy: {node.Method.Name}");
                break;

            case "Take":
                VisitTake(node);
                break;

            case "Skip":
                VisitSkip(node);
                break;

            case "Include":
                VisitInclude(node);
                break;

            case "First":
            case "FirstOrDefault":
                _descriptor.IsFirstQuery = true;
                _descriptor.Limit = 1;
                break;

            case "Single":
            case "SingleOrDefault":
                _descriptor.IsSingleQuery = true;
                _descriptor.Limit = 2; // Fetch 2 to verify single
                break;

            case "Count":
            case "LongCount":
                _descriptor.IsCountQuery = true;
                break;
        }

        return node;
    }

    /// <summary>
    /// Parse Where clause to extract filters (user, artist, period, dates).
    /// </summary>
    private void VisitWhere(MethodCallExpression node)
    {
        if (node.Arguments.Count < 2)
            return;

        var arg = node.Arguments[1];

        // Lambda expressions in LINQ expression trees are wrapped in Quote expressions
        // Unwrap the Quote to get the actual Lambda
        if (arg is UnaryExpression { NodeType: ExpressionType.Quote } quote)
            arg = quote.Operand;

        if (arg is not LambdaExpression lambda)
            return;

        // Extract conditions from lambda body
        ExtractConditions(lambda.Body);
    }

    /// <summary>
    /// Extract conditions from expression (handles ==, &&, Contains, etc.).
    /// </summary>
    private void ExtractConditions(Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression binary when binary.NodeType == ExpressionType.Equal:
                ExtractEqualityCondition(binary);
                break;

            case BinaryExpression binary when binary.NodeType == ExpressionType.AndAlso:
                // Handle AND conditions recursively
                ExtractConditions(binary.Left);
                ExtractConditions(binary.Right);
                break;

            case MethodCallExpression method when method.Method.Name == "Contains":
                // String.Contains not directly supported - client-side filter
                _descriptor.ClientSideFilters.Add($"Contains: {method}");
                break;

            case MethodCallExpression method when method.Method.Name == "StartsWith":
                // String.StartsWith not directly supported - client-side filter
                _descriptor.ClientSideFilters.Add($"StartsWith: {method}");
                break;

            default:
                // Unsupported filter - will be applied client-side
                _descriptor.ClientSideFilters.Add($"Unsupported filter: {expression}");
                break;
        }
    }

    /// <summary>
    /// Extract value from equality comparison (e.g., a.User == "smarshal").
    /// </summary>
    private void ExtractEqualityCondition(BinaryExpression binary)
    {
        // Determine which side is property access and which is value
        var (propertyName, value) = binary.Left is MemberExpression member
            ? (member.Member.Name, GetConstantValue(binary.Right))
            : binary.Right is MemberExpression rightMember
                ? (rightMember.Member.Name, GetConstantValue(binary.Left))
                : (null, null);

        if (propertyName == null || value == null)
            return;

        // Map property names to descriptor fields
        System.Diagnostics.Debug.WriteLine($"[ExtractEqualityCondition] propertyName={propertyName}, value={value}, entityType={_entityType.Name}");

        switch (propertyName)
        {
            case "User":
                _descriptor.User = value.ToString();
                System.Diagnostics.Debug.WriteLine($"[ExtractEqualityCondition] Set User={_descriptor.User}");
                break;

            case "Name" when _entityType == typeof(Artist):
                // Artist name filter - changes query type to specific artist
                // Normalize apostrophes for consistent API querying
                _descriptor.Artist = StringNormalizer.NormalizeArtistName(value.ToString());
                _descriptor.QueryType = QueryType.ArtistTracks; // May be overridden
                System.Diagnostics.Debug.WriteLine($"[ExtractEqualityCondition] Set Artist (from Name)={_descriptor.Artist}");
                break;

            case "ArtistName" when _entityType == typeof(Track) || _entityType == typeof(Album):
                // Normalize apostrophes for consistent API querying
                _descriptor.Artist = StringNormalizer.NormalizeArtistName(value.ToString());
                System.Diagnostics.Debug.WriteLine($"[ExtractEqualityCondition] Set Artist (from ArtistName)={_descriptor.Artist}");
                break;

            case "Period":
                if (value is LastFmPeriod period)
                    _descriptor.Period = period;
                break;

            case "PlayedAt":
                // Date comparison for RecentTracks
                // Would need to handle >= and <= separately
                _descriptor.ClientSideFilters.Add($"PlayedAt comparison: {binary}");
                break;

            default:
                _descriptor.ClientSideFilters.Add($"Unsupported property filter: {propertyName}");
                break;
        }
    }

    /// <summary>
    /// Parse OrderBy/OrderByDescending to extract sort field and direction.
    /// </summary>
    private void VisitOrderBy(MethodCallExpression node)
    {
        if (node.Arguments.Count < 2 || node.Arguments[1] is not LambdaExpression lambda)
            return;

        // Extract property name from lambda (e.g., a => a.PlayCount)
        if (lambda.Body is MemberExpression member)
        {
            _descriptor.SortBy = member.Member.Name;
            _descriptor.SortDirection = node.Method.Name == "OrderByDescending" ? "Descending" : "Ascending";
        }
    }

    /// <summary>
    /// Parse Take to extract limit.
    /// </summary>
    private void VisitTake(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2 && GetConstantValue(node.Arguments[1]) is int limit)
        {
            _descriptor.Limit = limit;
        }
    }

    /// <summary>
    /// Parse Skip to extract offset (will be converted to page number).
    /// </summary>
    private void VisitSkip(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2 && GetConstantValue(node.Arguments[1]) is int skip)
        {
            _descriptor.Skip = skip;
        }
    }

    /// <summary>
    /// Parse Include to extract navigation properties to load.
    /// </summary>
    private void VisitInclude(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 2)
        {
            // Extract property name from Include expression
            var includePath = GetIncludePath(node.Arguments[1]);
            if (includePath != null)
            {
                _descriptor.Includes.Add(includePath);
            }
        }
    }

    /// <summary>
    /// Get constant value from expression (handles ConstantExpression and MemberExpression).
    /// </summary>
    private static object? GetConstantValue(Expression expression)
    {
        // Try to compile and execute the expression to get the value
        // This handles all expression types including const strings, captured variables, etc.
        try
        {
            var lambda = Expression.Lambda(expression);
            var compiled = lambda.Compile();
            return compiled.DynamicInvoke();
        }
        catch
        {
            // Fall back to manual extraction for cases that can't be compiled
        }

        switch (expression)
        {
            case ConstantExpression constant:
                return constant.Value;

            case MemberExpression member when member.Expression is ConstantExpression constant:
                // Handle captured variables (e.g., var user = "smarshal"; a.User == user)
                // Can be either FieldInfo (captured variables) or PropertyInfo (const variables)
                if (member.Member is System.Reflection.FieldInfo field)
                {
                    return field.GetValue(constant.Value);
                }
                else if (member.Member is System.Reflection.PropertyInfo property)
                {
                    return property.GetValue(constant.Value);
                }
                return null;

            case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                // Handle type conversions
                return GetConstantValue(unary.Operand);

            default:
                return null;
        }
    }

    /// <summary>
    /// Get navigation property path from Include expression.
    /// </summary>
    private static string? GetIncludePath(Expression expression)
    {
        switch (expression)
        {
            case LambdaExpression lambda when lambda.Body is MemberExpression member:
                return member.Member.Name;

            case ConstantExpression constant when constant.Value is string path:
                return path;

            default:
                return null;
        }
    }

    /// <summary>
    /// Determine query type from entity type.
    /// </summary>
    private static QueryType DetermineQueryType(Type entityType)
    {
        if (entityType == typeof(Artist))
            return QueryType.TopArtists;

        if (entityType == typeof(Track))
            return QueryType.TopTracks;

        if (entityType == typeof(Album))
            return QueryType.TopAlbums;

        if (entityType == typeof(RecentTrack))
            return QueryType.RecentTracks;

        return QueryType.Unknown;
    }
}
