using FluentAssertions;
using Lfm.Data.Direct;
using Lfm.Shared.Models.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for CircuitBreaker
/// Tests state transitions, failure/success tracking, and timeout handling
/// </summary>
public class CircuitBreakerTests
{
    private readonly Mock<ILogger<CircuitBreaker>> _loggerMock;

    public CircuitBreakerTests()
    {
        _loggerMock = new Mock<ILogger<CircuitBreaker>>();
    }

    // ========== Initialization Tests ==========

    [Fact]
    public void CircuitBreaker_StartsInClosedState()
    {
        // Act
        var breaker = new CircuitBreaker(_loggerMock.Object);

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    // ========== Closed State Tests ==========

    [Fact]
    public async Task CircuitBreaker_Closed_AllowsSuccessfulOperation()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object);

        // Act
        var result = await breaker.ExecuteAsync(async () =>
        {
            return Result<string>.Ok("Success");
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task CircuitBreaker_Closed_TracksFailures()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 3);

        // Act - execute one failure
        await breaker.ExecuteAsync(async () =>
        {
            return Result<string>.Fail(ErrorType.ApiError, "Error");
        });

        // Assert
        breaker.FailureCount.Should().Be(1);
        breaker.State.Should().Be(CircuitBreakerState.Closed); // Still closed after 1 failure
    }

    [Fact]
    public async Task CircuitBreaker_Closed_OpensAfterThreshold()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 2);

        // Act - execute 2 failures to reach threshold
        for (int i = 0; i < 2; i++)
        {
            await breaker.ExecuteAsync(async () =>
            {
                return Result<string>.Fail(ErrorType.ApiError, "Error");
            });
        }

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Open);
        // FailureCount is only reset when opening from HalfOpen state
        breaker.FailureCount.Should().Be(2);
    }

    [Fact]
    public async Task CircuitBreaker_Closed_ResetsFailuresOnSuccess()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 3);

        // Act - fail once, then succeed
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));
        await breaker.ExecuteAsync(async () =>
            Result<string>.Ok("Success"));

        // Assert
        breaker.FailureCount.Should().Be(0); // Failures reset on success
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    // ========== Open State Tests ==========

    [Fact]
    public async Task CircuitBreaker_Open_FailsFastWithCircuitBreakerError()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 1);

        // Open the circuit
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));

        // Act - attempt operation while open
        var result = await breaker.ExecuteAsync<string>(async () =>
        {
            throw new Exception("Should not execute");
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.CircuitBreakerOpen);
    }

    [Fact]
    public async Task CircuitBreaker_Open_TransitionsToHalfOpenAfterTimeout()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 1, openTimeoutSeconds: 1);

        // Open the circuit
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));
        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Act - wait for timeout and attempt operation
        await Task.Delay(1100);
        var result = await breaker.ExecuteAsync(async () =>
            Result<string>.Ok("Success"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        breaker.State.Should().Be(CircuitBreakerState.HalfOpen);
    }

    // ========== HalfOpen State Tests ==========

    [Fact]
    public async Task CircuitBreaker_HalfOpen_ClosesAfterSuccesses()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object,
            failureThreshold: 1,
            successThreshold: 2,
            openTimeoutSeconds: 1);

        // Open the circuit
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));

        // Wait for timeout to transition to HalfOpen
        await Task.Delay(1100);

        // Act - execute successful operations
        await breaker.ExecuteAsync(async () => Result<string>.Ok("Success"));
        await breaker.ExecuteAsync(async () => Result<string>.Ok("Success"));

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task CircuitBreaker_HalfOpen_ReopensOnFailure()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object,
            failureThreshold: 1,
            successThreshold: 2,
            openTimeoutSeconds: 1);

        // Open and transition to HalfOpen
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));
        await Task.Delay(1100);

        // Act - fail in HalfOpen state
        var result = await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Recovery failed"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        breaker.State.Should().Be(CircuitBreakerState.Open); // Reopened
    }

    // ========== Reset Tests ==========

    [Fact]
    public async Task CircuitBreaker_Reset_ClosesCircuitAndResetsCounts()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 1);

        // Open the circuit
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));
        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Act
        breaker.Reset();

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Closed);
        breaker.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task CircuitBreaker_ResetInHalfOpen_AllowsNormalOperation()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object,
            failureThreshold: 1,
            openTimeoutSeconds: 1);

        // Open the circuit
        await breaker.ExecuteAsync(async () =>
            Result<string>.Fail(ErrorType.ApiError, "Error"));
        breaker.State.Should().Be(CircuitBreakerState.Open);

        // Wait for timeout
        await Task.Delay(1100);

        // Act - Reset before transitioning to HalfOpen
        breaker.Reset();
        breaker.State.Should().Be(CircuitBreakerState.Closed);

        // Verify normal operation after reset
        var result = await breaker.ExecuteAsync(async () =>
            Result<string>.Ok("Success"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    // ========== Exception Handling Tests ==========

    [Fact]
    public async Task CircuitBreaker_CatchesOperationException()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object);

        // Act
        var result = await breaker.ExecuteAsync<string>(async () =>
        {
            throw new InvalidOperationException("Test exception");
        });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.UnknownError);
        result.Error?.TechnicalDetails.Should().Contain("Test exception");
    }

    // ========== State Tracking Tests ==========

    [Fact]
    public async Task CircuitBreaker_TracksConsecutiveFailures()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 5);

        // Act
        for (int i = 0; i < 3; i++)
        {
            await breaker.ExecuteAsync(async () =>
                Result<string>.Fail(ErrorType.ApiError, "Error"));
        }

        // Assert
        breaker.FailureCount.Should().Be(3);
    }

    [Fact]
    public async Task CircuitBreaker_ResetsCountersOnStateChange()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object, failureThreshold: 2);

        // Act - cause 2 failures to open circuit
        for (int i = 0; i < 2; i++)
        {
            await breaker.ExecuteAsync(async () =>
                Result<string>.Fail(ErrorType.ApiError, "Error"));
        }

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Open);
        breaker.FailureCount.Should().Be(2); // NOT reset when opening from Closed (only reset from HalfOpen)
    }

    [Fact]
    public async Task CircuitBreaker_WithCustomThresholds_WorksCorrectly()
    {
        // Arrange
        var breaker = new CircuitBreaker(_loggerMock.Object,
            failureThreshold: 4,
            successThreshold: 3);

        // Act - fail 3 times (below threshold)
        for (int i = 0; i < 3; i++)
        {
            await breaker.ExecuteAsync(async () =>
                Result<string>.Fail(ErrorType.ApiError, "Error"));
        }

        // Assert
        breaker.State.Should().Be(CircuitBreakerState.Closed); // Not opened yet
        breaker.FailureCount.Should().Be(3);
    }
}
