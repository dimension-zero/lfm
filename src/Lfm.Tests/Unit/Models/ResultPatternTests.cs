using FluentAssertions;
using Lfm.Core.Models.Results;

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
}
