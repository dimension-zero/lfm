using FluentAssertions;
using Lfm.Framework.Abstractions;
using Lfm.Shared.Models.Results;
using Lfm.Wine.Actions;
using Lfm.Wine.Models;
using Xunit;

namespace Lfm.Tests.Unit.Wine;

/// <summary>
/// Unit tests for WineActionExecutor implementing IActionExecutor<WineItem>
/// Tests wine-specific actions: add to cellar, rate, purchase, view details, etc.
/// Note: Result (non-generic) uses .Success, not .IsSuccess
/// </summary>
public class WineActionExecutorTests
{
    private readonly WineActionExecutor _executor;
    private readonly WineItem _sampleWine;

    public WineActionExecutorTests()
    {
        _executor = new WineActionExecutor();
        _sampleWine = new WineItem
        {
            Type = "red",
            Id = "wine-001",
            Name = "Test Pinot Noir",
            ProducerName = "Test Vineyard",
            Vintage = 2020,
            Region = "Oregon",
            Variety = "Pinot Noir",
            UserRating = 4.5f,
            UserTastingCount = 5
        };
    }

    // ========== Initialization Tests ==========

    [Fact]
    public void WineActionExecutor_Initializes()
    {
        // Act & Assert
        var executor = new WineActionExecutor();
        executor.Should().NotBeNull();
    }

    // ========== GetAvailableActionsAsync Tests ==========

    [Fact]
    public async Task GetAvailableActionsAsync_ReturnsActionsList()
    {
        // Act
        var result = await _executor.GetAvailableActionsAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableActionsAsync_ContainsExpectedActions()
    {
        // Act
        var result = await _executor.GetAvailableActionsAsync();

        // Assert
        result.Success.Should().BeTrue();
        var actions = result.Data;
        actions.Should().Contain("addcellar");
        actions.Should().Contain("rate");
        actions.Should().Contain("purchase");
        actions.Should().Contain("viewdetails");
        actions.Should().Contain("viewsimilar");
        actions.Should().Contain("exportwinelist");
    }

    // ========== IsActionAvailableAsync Tests ==========

    [Fact]
    public async Task IsActionAvailableAsync_ReturnsTrueForValidActions()
    {
        // Act
        var addCellarResult = await _executor.IsActionAvailableAsync("addcellar");
        var rateResult = await _executor.IsActionAvailableAsync("rate");

        // Assert
        addCellarResult.IsSuccess.Should().BeTrue();
        addCellarResult.Data.Should().BeTrue();
        rateResult.IsSuccess.Should().BeTrue();
        rateResult.Data.Should().BeTrue();
    }

    [Fact]
    public async Task IsActionAvailableAsync_ReturnsFalseForInvalidActions()
    {
        // Act
        var result = await _executor.IsActionAvailableAsync("unknownaction");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task IsActionAvailableAsync_IsCaseInsensitive()
    {
        // Act
        var lowerResult = await _executor.IsActionAvailableAsync("addcellar");
        var upperResult = await _executor.IsActionAvailableAsync("ADDCELLAR");
        var mixedResult = await _executor.IsActionAvailableAsync("AddCellar");

        // Assert
        lowerResult.IsSuccess.Should().BeTrue();
        lowerResult.Data.Should().BeTrue();
        upperResult.IsSuccess.Should().BeTrue();
        upperResult.Data.Should().BeTrue();
        mixedResult.IsSuccess.Should().BeTrue();
        mixedResult.Data.Should().BeTrue();
    }

    // ========== ExecuteActionAsync Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_WithValidWineAndAction_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "addcellar");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_WithNullWine_Fails()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(null!, "addcellar");

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
        result.Error?.Message.Should().Contain("null");
    }

    [Fact]
    public async Task ExecuteActionAsync_WithUnknownAction_Fails()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "unknownaction");

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
        result.Error?.Message.Should().Contain("Unknown");
    }

    // ========== Action: AddCellar Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_AddCellar_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "addcellar");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_AddCellar_IsCaseInsensitive()
    {
        // Act
        var lowerResult = await _executor.ExecuteActionAsync(_sampleWine, "addcellar");
        var upperResult = await _executor.ExecuteActionAsync(_sampleWine, "ADDCELLAR");

        // Assert
        lowerResult.Success.Should().BeTrue();
        upperResult.Success.Should().BeTrue();
    }

    // ========== Action: Rate Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_Rate_SucceedsWithValidRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", 4.5f } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_SucceedsWithIntRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", 4 } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_SucceedsWithStringRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", "4.5" } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_FailsWithoutParameter()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate");

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
        result.Error?.Message.Should().Contain("Rating");
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_FailsWithInvalidRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", 10.0f } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
        result.Error?.Message.Should().Contain("1 and 5");
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_FailsWithZeroRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", 0.0f } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_FailsWithNegativeRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", -1.0f } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_SucceedsAtBoundaryValues()
    {
        // Arrange
        var minRating = new Dictionary<string, object> { { "rating", 1 } };
        var maxRating = new Dictionary<string, object> { { "rating", 5 } };

        // Act
        var minResult = await _executor.ExecuteActionAsync(_sampleWine, "rate", minRating);
        var maxResult = await _executor.ExecuteActionAsync(_sampleWine, "rate", maxRating);

        // Assert
        minResult.Success.Should().BeTrue();
        maxResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_Rate_FailsWithNonNumericRating()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "rating", "not a number" } };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeFalse();
        result.Error?.Type.Should().Be(ErrorType.ValidationError);
    }

    // ========== Action: Purchase Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_Purchase_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "purchase");

        // Assert
        result.Success.Should().BeTrue();
    }

    // ========== Action: ViewDetails Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_ViewDetails_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "viewdetails");

        // Assert
        result.Success.Should().BeTrue();
    }

    // ========== Action: ViewSimilar Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_ViewSimilar_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "viewsimilar");

        // Assert
        result.Success.Should().BeTrue();
    }

    // ========== Action: ExportWineList Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_ExportWineList_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "exportwinelist");

        // Assert
        result.Success.Should().BeTrue();
    }

    // ========== Exception Handling Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_CatchesAndReturnsExceptions()
    {
        // Arrange - Create a wine with properties that might cause issues
        var wine = new WineItem { Id = null!, Name = null! };

        // Act
        var result = await _executor.ExecuteActionAsync(wine, "addcellar");

        // Assert
        // Should handle gracefully without throwing
        result.Should().NotBeNull();
    }

    // ========== Integration Tests ==========

    [Fact]
    public async Task Executor_AllActionsExist()
    {
        // Arrange
        var availableActions = await _executor.GetAvailableActionsAsync();

        // Act & Assert
        foreach (var action in availableActions.Data)
        {
            var isAvailable = await _executor.IsActionAvailableAsync(action);
            isAvailable.IsSuccess.Should().BeTrue();
            isAvailable.Data.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Executor_ExecuteAvailableAction()
    {
        // Arrange
        var availableActions = await _executor.GetAvailableActionsAsync();
        var action = availableActions.Data.First();

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, action);

        // Assert
        result.Should().NotBeNull();
    }

    // ========== Parameter Handling Tests ==========

    [Fact]
    public async Task ExecuteActionAsync_WithEmptyParameters_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "addcellar", new Dictionary<string, object>());

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_WithExtraParameters_Succeeds()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "rating", 4.5f },
            { "extra", "ignored" },
            { "another", 123 }
        };

        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "rate", parameters);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteActionAsync_WithNullParameters_Succeeds()
    {
        // Act
        var result = await _executor.ExecuteActionAsync(_sampleWine, "addcellar", null);

        // Assert
        result.Success.Should().BeTrue();
    }
}
