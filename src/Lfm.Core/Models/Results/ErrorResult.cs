namespace Lfm.Core.Models.Results;

/// <summary>
/// Represents detailed error information for failed operations
/// </summary>
public class ErrorResult
{
    public ErrorResult(ErrorType type, string message, string? technicalDetails = null)
    {
        Type = type;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        TechnicalDetails = technicalDetails;
    }

    /// <summary>
    /// The category/type of error that occurred
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// User-friendly error message describing what went wrong
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Technical details for debugging (optional, not shown to end users)
    /// </summary>
    public string? TechnicalDetails { get; }

    /// <summary>
    /// Returns true if this is a retryable error (network issues, rate limits, circuit breaker, etc.)
    /// </summary>
    public bool IsRetryable => Type is ErrorType.ApiError or ErrorType.NetworkError or ErrorType.CircuitBreakerOpen;

    /// <summary>
    /// Returns true if this is a user error that requires action (missing config, invalid input, etc.)
    /// </summary>
    public bool RequiresUserAction => Type is ErrorType.ConfigurationError or ErrorType.ValidationError;

    /// <summary>
    /// Gets the appropriate display symbol for this error type
    /// </summary>
    public string GetDisplaySymbol(bool useUnicode = true)
    {
        return Type switch
        {
            ErrorType.ApiError => useUnicode ? "🌐" : "[API]",
            ErrorType.ValidationError => useUnicode ? "⚠️" : "[WARN]",
            ErrorType.DataError => useUnicode ? "📄" : "[DATA]",
            ErrorType.ConfigurationError => useUnicode ? "⚙️" : "[CONFIG]",
            ErrorType.NetworkError => useUnicode ? "🔌" : "[NET]",
            ErrorType.AuthenticationError => useUnicode ? "🔐" : "[AUTH]",
            ErrorType.RateLimitError => useUnicode ? "⏱️" : "[RATE]",
            ErrorType.CircuitBreakerOpen => useUnicode ? "🔴" : "[CIRCUIT]",
            ErrorType.UnknownError => useUnicode ? "❓" : "[UNKNOWN]",
            _ => useUnicode ? "❌" : "[ERROR]"
        };
    }

    public override string ToString()
    {
        var result = $"[{Type}] {Message}";
        if (!string.IsNullOrEmpty(TechnicalDetails))
            result += $" (Details: {TechnicalDetails})";
        return result;
    }
}

/// <summary>
/// Classification of error types for consistent error handling
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Last.fm API returned an error response
    /// </summary>
    ApiError,

    /// <summary>
    /// User provided invalid input or parameters
    /// </summary>
    ValidationError,

    /// <summary>
    /// No data found or data processing failed
    /// </summary>
    DataError,

    /// <summary>
    /// Missing or invalid configuration (API key, username, etc.)
    /// </summary>
    ConfigurationError,

    /// <summary>
    /// Network connectivity issues
    /// </summary>
    NetworkError,

    /// <summary>
    /// Authentication or authorization failed
    /// </summary>
    AuthenticationError,

    /// <summary>
    /// Rate limit exceeded
    /// </summary>
    RateLimitError,

    /// <summary>
    /// Circuit breaker is open due to repeated failures
    /// </summary>
    CircuitBreakerOpen,

    /// <summary>
    /// Unexpected error that doesn't fit other categories
    /// </summary>
    UnknownError
}