using Lfm.Shared.Models.Results;
using Microsoft.Extensions.Logging;

namespace Lfm.Data.Direct;

/// <summary>
/// Circuit breaker implementation for API resilience.
/// Tracks failures and opens circuit to prevent cascading failures.
/// Uses Result&lt;T&gt; pattern for consistent error handling.
/// </summary>
public class CircuitBreaker : ICircuitBreaker
{
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly int _failureThreshold;
    private readonly int _successThreshold;
    private readonly TimeSpan _openTimeout;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _consecutiveFailures = 0;
    private int _consecutiveSuccesses = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;

    public CircuitBreakerState State => _state;
    public int FailureCount => _consecutiveFailures;

    /// <summary>
    /// Create a circuit breaker with specified thresholds
    /// </summary>
    /// <param name="failureThreshold">Number of consecutive failures before opening circuit</param>
    /// <param name="successThreshold">Number of consecutive successes in HalfOpen before closing circuit</param>
    /// <param name="openTimeoutSeconds">Seconds to wait before transitioning from Open to HalfOpen</param>
    public CircuitBreaker(
        ILogger<CircuitBreaker> logger,
        int failureThreshold = 5,
        int successThreshold = 2,
        int openTimeoutSeconds = 60)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failureThreshold = failureThreshold;
        _successThreshold = successThreshold;
        _openTimeout = TimeSpan.FromSeconds(openTimeoutSeconds);

        _logger.LogDebug("Circuit breaker initialized: failureThreshold={FailureThreshold}, successThreshold={SuccessThreshold}, openTimeout={OpenTimeout}s",
            _failureThreshold, _successThreshold, openTimeoutSeconds);
    }

    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation) where T : class
    {
        await _stateLock.WaitAsync();
        try
        {
            // Check if we should transition from Open to HalfOpen
            if (_state == CircuitBreakerState.Open)
            {
                var timeSinceFailure = DateTime.UtcNow - _lastFailureTime;
                if (timeSinceFailure >= _openTimeout)
                {
                    _logger.LogInformation("Circuit breaker transitioning from Open to HalfOpen (timeout elapsed)");
                    _state = CircuitBreakerState.HalfOpen;
                    _consecutiveSuccesses = 0;
                }
                else
                {
                    var remainingSeconds = (_openTimeout - timeSinceFailure).TotalSeconds;
                    _logger.LogWarning("Circuit breaker is OPEN - failing fast (retry in {RemainingSeconds:F0}s)", remainingSeconds);
                    return Result<T>.Fail(ErrorType.CircuitBreakerOpen,
                        $"Circuit breaker is open due to repeated failures",
                        $"Retry in {remainingSeconds:F0} seconds");
                }
            }
        }
        finally
        {
            _stateLock.Release();
        }

        // Execute the operation
        Result<T> result;
        try
        {
            result = await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception in circuit breaker operation");
            result = Result<T>.Fail(ErrorType.UnknownError,
                "Unexpected error during operation",
                ex.Message);
        }

        // Record result and update state
        await _stateLock.WaitAsync();
        try
        {
            if (result.IsSuccess)
            {
                await HandleSuccessAsync();
            }
            else
            {
                // Error is guaranteed to be non-null when IsSuccess is false
                await HandleFailureAsync(result.Error!);
            }
        }
        finally
        {
            _stateLock.Release();
        }

        return result;
    }

    private async Task HandleSuccessAsync()
    {
        _consecutiveFailures = 0;

        if (_state == CircuitBreakerState.HalfOpen)
        {
            _consecutiveSuccesses++;
            _logger.LogDebug("Circuit breaker success in HalfOpen state ({Successes}/{Threshold})",
                _consecutiveSuccesses, _successThreshold);

            if (_consecutiveSuccesses >= _successThreshold)
            {
                _logger.LogInformation("Circuit breaker transitioning from HalfOpen to Closed (recovery confirmed)");
                _state = CircuitBreakerState.Closed;
                _consecutiveSuccesses = 0;
            }
        }

        await Task.CompletedTask;
    }

    private async Task HandleFailureAsync(ErrorResult error)
    {
        _consecutiveSuccesses = 0;
        _consecutiveFailures++;
        _lastFailureTime = DateTime.UtcNow;

        _logger.LogWarning("Circuit breaker recorded failure ({Failures}/{Threshold}): {ErrorType} - {Message}",
            _consecutiveFailures, _failureThreshold, error.Type, error.Message);

        if (_state == CircuitBreakerState.HalfOpen)
        {
            // Any failure in HalfOpen immediately reopens the circuit
            _logger.LogWarning("Circuit breaker transitioning from HalfOpen to Open (failure during recovery test)");
            _state = CircuitBreakerState.Open;
            _consecutiveFailures = 0; // Reset for next attempt
        }
        else if (_state == CircuitBreakerState.Closed && _consecutiveFailures >= _failureThreshold)
        {
            _logger.LogError("Circuit breaker OPENING (failure threshold {Threshold} exceeded)", _failureThreshold);
            _state = CircuitBreakerState.Open;
        }

        await Task.CompletedTask;
    }

    public void Reset()
    {
        _stateLock.Wait();
        try
        {
            _logger.LogInformation("Circuit breaker manually reset to Closed state");
            _state = CircuitBreakerState.Closed;
            _consecutiveFailures = 0;
            _consecutiveSuccesses = 0;
            _lastFailureTime = DateTime.MinValue;
        }
        finally
        {
            _stateLock.Release();
        }
    }
}
