using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Lfm.Shared.Models.Results;

namespace Lfm.Tests.Unit.Models;

/// <summary>
/// Unit tests for Result&lt;T&gt; pattern - core error handling used everywhere
/// </summary>
[Trait("Category", "Unit")]
public class ResultPatternTests
{
    #region Success Results Tests

    [Fact]
    public void Ok_CreatesSuccessfulResult()
    {
        // Arrange
        var data = "test data";

        // Act
        var result = Result<string>.Ok(data);

        // Assert
        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Data.Should().Be(data);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Ok_WithNullData_CreatesSuccessfulResultWithNull()
    {
        // Act
        var result = Result<string>.Ok(null!);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeNull();
    }

    #endregion

    #region Failure Results Tests

    [Fact]
    public void Fail_CreatesFailureResult()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ApiError, "Test error", null);

        // Act
        var result = Result<string>.Fail(error);

        // Assert
        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Data.Should().BeNull();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ApiError_CreatesApiErrorResult()
    {
        // Act
        var result = Result<string>.ApiError("API failed");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ApiError);
        result.Error.Message.Should().Be("API failed");
    }

    [Fact]
    public void ValidationError_CreatesValidationErrorResult()
    {
        // Act
        var result = Result<string>.ValidationError("Invalid input");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ValidationError);
        result.Error.Message.Should().Be("Invalid input");
    }

    [Fact]
    public void DataError_CreatesDataErrorResult()
    {
        // Act
        var result = Result<string>.DataError("Data corrupted");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.DataError);
        result.Error.Message.Should().Be("Data corrupted");
    }

    [Fact]
    public void ConfigurationError_CreatesConfigurationErrorResult()
    {
        // Act
        var result = Result<string>.ConfigurationError("Missing API key");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Type.Should().Be(ErrorType.ConfigurationError);
        result.Error.Message.Should().Be("Missing API key");
    }

    #endregion

    #region Map Tests

    [Fact]
    public void Map_WithSuccessfulResult_TransformsData()
    {
        // Arrange
        var result = Result<int>.Ok(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.Success.Should().BeTrue();
        mapped.Data.Should().Be("42");
    }

    [Fact]
    public void Map_WithFailedResult_PreservesError()
    {
        // Arrange
        var result = Result<int>.ApiError("Failed");

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.Success.Should().BeFalse();
        mapped.Error.Should().NotBeNull();
        mapped.Error!.Message.Should().Be("Failed");
        mapped.Data.Should().BeNull();
    }

    #endregion

    #region OnSuccess and OnFailure Tests

    [Fact]
    public void OnSuccess_WithSuccessfulResult_ExecutesAction()
    {
        // Arrange
        var result = Result<string>.Ok("test");
        var executed = false;
        string? capturedData = null;

        // Act
        var returned = result.OnSuccess(data =>
        {
            executed = true;
            capturedData = data;
        });

        // Assert
        executed.Should().BeTrue("action should execute for successful result");
        capturedData.Should().Be("test");
        returned.Should().Be(result, "should return same result for chaining");
    }

    [Fact]
    public void OnSuccess_WithFailedResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<string>.ApiError("Failed");
        var executed = false;

        // Act
        result.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeFalse("action should not execute for failed result");
    }

    [Fact]
    public void OnFailure_WithFailedResult_ExecutesAction()
    {
        // Arrange
        var result = Result<string>.ValidationError("Invalid input");
        var executed = false;
        ErrorResult? capturedError = null;

        // Act
        var returned = result.OnFailure(error =>
        {
            executed = true;
            capturedError = error;
        });

        // Assert
        executed.Should().BeTrue("action should execute for failed result");
        capturedError.Should().NotBeNull();
        capturedError!.Type.Should().Be(ErrorType.ValidationError);
        returned.Should().Be(result, "should return same result for chaining");
    }

    [Fact]
    public void OnFailure_WithSuccessfulResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<string>.Ok("test");
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeFalse("action should not execute for successful result");
    }

    [Fact]
    public void Chaining_OnSuccessAndOnFailure_ExecutesCorrectAction()
    {
        // Arrange
        var successResult = Result<int>.Ok(42);
        var failureResult = Result<int>.DataError("No data");
        var successExecuted = false;
        var failureExecuted = false;

        // Act
        successResult
            .OnSuccess(_ => successExecuted = true)
            .OnFailure(_ => failureExecuted = true);

        failureExecuted = false; // Reset
        failureResult
            .OnSuccess(_ => successExecuted = true)
            .OnFailure(_ => failureExecuted = true);

        // Assert
        successExecuted.Should().BeTrue("success action should execute for successful result");
        failureExecuted.Should().BeTrue("failure action should execute for failed result");
    }

    #endregion

    #region Property Aliases Tests

    [Fact]
    public void Value_ReturnsData()
    {
        // Arrange
        var result = Result<string>.Ok("test data");

        // Assert
        result.Value.Should().Be(result.Data);
        result.Value.Should().Be("test data");
    }

    [Fact]
    public void IsSuccess_ReturnsSuccess()
    {
        // Arrange
        var successResult = Result<string>.Ok("test");
        var failureResult = Result<string>.ApiError("Failed");

        // Assert
        successResult.IsSuccess.Should().Be(successResult.Success);
        successResult.IsSuccess.Should().BeTrue();
        failureResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ErrorMessage_ReturnsErrorMessage()
    {
        // Arrange
        var failureResult = Result<string>.ValidationError("Test error message");
        var successResult = Result<string>.Ok("test");

        // Assert
        failureResult.ErrorMessage.Should().Be("Test error message");
        successResult.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region Non-Generic Result Tests

    [Fact]
    public void NonGenericResult_Ok_CreatesSuccessfulResult()
    {
        // Act
        var result = Result.Ok();

        // Assert
        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void NonGenericResult_Fail_CreatesFailureResult()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ConfigurationError, "Config missing");

        // Act
        var result = Result.Fail(error);

        // Assert
        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void NonGenericResult_ApiError_CreatesApiErrorResult()
    {
        // Act
        var result = Result.ApiError("API unavailable");

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.ApiError);
        result.Error.Message.Should().Be("API unavailable");
    }

    [Fact]
    public void NonGenericResult_OnFailure_ExecutesActionForFailure()
    {
        // Arrange
        var result = Result.ValidationError("Invalid");
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    #endregion

    #region ErrorResult Tests

    [Fact]
    public void ErrorResult_Constructor_SetsProperties()
    {
        // Act
        var error = new ErrorResult(ErrorType.NetworkError, "Connection lost", "Technical: timeout");

        // Assert
        error.Type.Should().Be(ErrorType.NetworkError);
        error.Message.Should().Be("Connection lost");
        error.TechnicalDetails.Should().Be("Technical: timeout");
    }

    [Fact]
    public void ErrorResult_IsRetryable_ReturnsTrueForRetryableErrors()
    {
        // Arrange
        var apiError = new ErrorResult(ErrorType.ApiError, "API failed");
        var networkError = new ErrorResult(ErrorType.NetworkError, "Connection lost");
        var validationError = new ErrorResult(ErrorType.ValidationError, "Invalid input");

        // Assert
        apiError.IsRetryable.Should().BeTrue("API errors are retryable");
        networkError.IsRetryable.Should().BeTrue("Network errors are retryable");
        validationError.IsRetryable.Should().BeFalse("Validation errors are not retryable");
    }

    [Fact]
    public void ErrorResult_RequiresUserAction_ReturnsTrueForUserActionableErrors()
    {
        // Arrange
        var configError = new ErrorResult(ErrorType.ConfigurationError, "Missing API key");
        var validationError = new ErrorResult(ErrorType.ValidationError, "Invalid format");
        var apiError = new ErrorResult(ErrorType.ApiError, "Server error");

        // Assert
        configError.RequiresUserAction.Should().BeTrue("Config errors require user action");
        validationError.RequiresUserAction.Should().BeTrue("Validation errors require user action");
        apiError.RequiresUserAction.Should().BeFalse("API errors don't require user action");
    }

    [Fact]
    public void ErrorResult_GetDisplaySymbol_ReturnsCorrectSymbolForUnicode()
    {
        // Arrange
        var errors = new[]
        {
            (ErrorType.ApiError, "🌐"),
            (ErrorType.ValidationError, "⚠️"),
            (ErrorType.DataError, "📄"),
            (ErrorType.ConfigurationError, "⚙️"),
            (ErrorType.NetworkError, "🔌"),
            (ErrorType.AuthenticationError, "🔐"),
            (ErrorType.RateLimitError, "⏱️"),
            (ErrorType.UnknownError, "❓")
        };

        // Act & Assert
        foreach (var (type, expectedSymbol) in errors)
        {
            var error = new ErrorResult(type, "test");
            error.GetDisplaySymbol(useUnicode: true).Should().Be(expectedSymbol,
                $"ErrorType.{type} should have symbol {expectedSymbol}");
        }
    }

    [Fact]
    public void ErrorResult_GetDisplaySymbol_ReturnsCorrectSymbolForAscii()
    {
        // Arrange
        var errors = new[]
        {
            (ErrorType.ApiError, "[API]"),
            (ErrorType.ValidationError, "[WARN]"),
            (ErrorType.DataError, "[DATA]"),
            (ErrorType.ConfigurationError, "[CONFIG]"),
            (ErrorType.NetworkError, "[NET]"),
            (ErrorType.AuthenticationError, "[AUTH]"),
            (ErrorType.RateLimitError, "[RATE]"),
            (ErrorType.UnknownError, "[UNKNOWN]")
        };

        // Act & Assert
        foreach (var (type, expectedSymbol) in errors)
        {
            var error = new ErrorResult(type, "test");
            error.GetDisplaySymbol(useUnicode: false).Should().Be(expectedSymbol,
                $"ErrorType.{type} should have ASCII symbol {expectedSymbol}");
        }
    }

    [Fact]
    public void ErrorResult_ToString_FormatsCorrectly()
    {
        // Act
        var errorWithoutDetails = new ErrorResult(ErrorType.DataError, "Parse failed");
        var errorWithDetails = new ErrorResult(ErrorType.NetworkError, "Connection timeout",
            "Socket error 10060");

        // Assert
        errorWithoutDetails.ToString().Should().Be("[DataError] Parse failed");
        errorWithDetails.ToString().Should().Be("[NetworkError] Connection timeout (Details: Socket error 10060)");
    }

    [Fact]
    public void ErrorResult_Constructor_ThrowsOnNullMessage()
    {
        // Act
        Action act = () => new ErrorResult(ErrorType.ApiError, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("message");
    }

    #endregion

    #region Technical Details Tests

    [Fact]
    public void Result_WithTechnicalDetails_PreservesTechnicalInformation()
    {
        // Arrange
        var technicalInfo = "Stack trace: at Line 42";

        // Act
        var result = Result<string>.DataError("Failed to parse", technicalInfo);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be("Failed to parse");
        result.Error.TechnicalDetails.Should().Be(technicalInfo);
    }

    [Fact]
    public void Result_FactoryMethods_SupportTechnicalDetails()
    {
        // Act
        var apiError = Result<int>.ApiError("API failed", "HTTP 500");
        var validationError = Result<int>.ValidationError("Invalid", "Field: username");
        var dataError = Result<int>.DataError("No data", "Query returned empty");
        var configError = Result<int>.ConfigurationError("Missing key", "Key: api_secret");

        // Assert
        apiError.Error!.TechnicalDetails.Should().Be("HTTP 500");
        validationError.Error!.TechnicalDetails.Should().Be("Field: username");
        dataError.Error!.TechnicalDetails.Should().Be("Query returned empty");
        configError.Error!.TechnicalDetails.Should().Be("Key: api_secret");
    }

    #endregion
}
