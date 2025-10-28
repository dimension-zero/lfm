using Lfm.Shared.Models.Results;

namespace Lfm.Core.Services;

/// <summary>
/// Circuit breaker pattern for API resilience.
/// Prevents cascading failures by tracking API health and opening circuit when failure threshold is reached.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Execute an operation through the circuit breaker.
    /// If circuit is open, returns failure immediately without executing operation.
    /// </summary>
    Task<Result<T>> ExecuteAsync<T>(Func<Task<Result<T>>> operation) where T : class;

    /// <summary>
    /// Current state of the circuit breaker (Closed, Open, HalfOpen)
    /// </summary>
    CircuitBreakerState State { get; }

    /// <summary>
    /// Number of consecutive failures recorded
    /// </summary>
    int FailureCount { get; }

    /// <summary>
    /// Reset the circuit breaker to closed state (for testing/manual intervention)
    /// </summary>
    void Reset();
}

/// <summary>
/// Circuit breaker states
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - requests flow through
    /// </summary>
    Closed,

    /// <summary>
    /// Failure threshold exceeded - requests fail immediately
    /// </summary>
    Open,

    /// <summary>
    /// Testing recovery - limited requests allowed to test if service recovered
    /// </summary>
    HalfOpen
}
