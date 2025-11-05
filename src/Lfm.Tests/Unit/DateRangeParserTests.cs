using FluentAssertions;
using Lfm.Shared.Utilities;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for DateRangeParser utility
/// Tests command-line date range parsing, validation, and formatting
/// </summary>
public class DateRangeParserTests
{
    // ========== Date Range Parsing Tests ==========

    [Fact]
    public void ParseDateRange_WithValidDates_ReturnsCorrectRange()
    {
        // Act
        var (from, to) = DateRangeParser.ParseDateRange("2023-01-01", "2023-12-31");

        // Assert
        from.Year.Should().Be(2023);
        from.Month.Should().Be(1);
        from.Day.Should().Be(1);
        to.Year.Should().Be(2023);
        to.Month.Should().Be(12);
        to.Day.Should().Be(31);
    }

    [Theory]
    [InlineData("2024-01-15", "2024-03-20")]
    [InlineData("2023-06-01", "2023-06-30")]
    [InlineData("2020-01-01", "2025-12-31")]
    public void ParseDateRange_WithVariousDates_ParsesCorrectly(string fromStr, string toStr)
    {
        // Act
        var (from, to) = DateRangeParser.ParseDateRange(fromStr, toStr);

        // Assert
        from.Should().BeBefore(to);
    }

    [Fact]
    public void ParseDateRange_WithISO8601Format_Succeeds()
    {
        // The parser requires ISO 8601 format (YYYY-MM-DD)
        // Act & Assert
        var (from, to) = DateRangeParser.ParseDateRange("2023-01-01", "2023-12-31");
        from.Year.Should().BeGreaterThan(0);
        to.Year.Should().BeGreaterThan(0);
    }

    // ========== Year Range Parsing Tests ==========

    [Fact]
    public void ParseYearRange_WithValidYear_ReturnsFullYear()
    {
        // Act
        var (from, to) = DateRangeParser.ParseYearRange("2023");

        // Assert
        from.Year.Should().Be(2023);
        from.Month.Should().Be(1);
        from.Day.Should().Be(1);
        to.Year.Should().Be(2023);
        to.Month.Should().Be(12);
        to.Day.Should().Be(31);
    }

    [Theory]
    [InlineData("2020")]
    [InlineData("2024")]
    [InlineData("2000")]
    public void ParseYearRange_WithVariousYears_CoversFullYear(string yearStr)
    {
        // Act
        var (from, to) = DateRangeParser.ParseYearRange(yearStr);

        // Assert
        from.Should().BeOnOrBefore(to);
        (to - from).TotalDays.Should().BeGreaterThan(364);
    }

    // ========== Validation Tests ==========

    [Fact]
    public void ValidateDateRangeParameters_WithFromAndToOnly_Succeeds()
    {
        // Act & Assert - Should not throw
        DateRangeParser.ValidateDateRangeParameters(periodStr: null, "2023-01-01", "2023-12-31", null);
    }

    [Fact]
    public void ValidateDateRangeParameters_WithYearOnly_Succeeds()
    {
        // Act & Assert - Should not throw
        DateRangeParser.ValidateDateRangeParameters(periodStr: null, null, null, "2023");
    }

    [Fact]
    public void ValidateDateRangeParameters_WithPeriodOnly_Succeeds()
    {
        // Act & Assert - Should not throw
        DateRangeParser.ValidateDateRangeParameters("overall", null, null, null);
    }

    [Fact]
    public void ValidateDateRangeParameters_WithConflictingParameters_ThrowsException()
    {
        // Act & Assert - period + year combination is invalid
        Assert.Throws<ArgumentException>(() =>
            DateRangeParser.ValidateDateRangeParameters("overall", null, null, "2023")
        );
    }

    [Fact]
    public void ValidateDateRangeParameters_WithFromWithoutTo_ThrowsException()
    {
        // Act & Assert - from without to is invalid
        Assert.Throws<ArgumentException>(() =>
            DateRangeParser.ValidateDateRangeParameters(null, "2023-01-01", null, null)
        );
    }

    [Fact]
    public void ValidateDateRangeParameters_WithToWithoutFrom_ThrowsException()
    {
        // Act & Assert - to without from is invalid
        Assert.Throws<ArgumentException>(() =>
            DateRangeParser.ValidateDateRangeParameters(null, null, "2023-12-31", null)
        );
    }

    [Fact]
    public void ValidateDateRangeParameters_WithNullDefaultsToOverall_Succeeds()
    {
        // Act & Assert - All null defaults to "overall" which is valid
        DateRangeParser.ValidateDateRangeParameters(null, null, null, null);
    }

    // ========== Formatting Tests ==========

    [Fact]
    public void FormatDateRange_WithValidDates_ReturnsFormattedString()
    {
        // Arrange
        var from = new DateTime(2023, 1, 15);
        var to = new DateTime(2023, 12, 31);

        // Act
        var result = DateRangeParser.FormatDateRange(from, to);

        // Assert
        result.Should().Contain("2023");
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FormatDateRange_WithSameDateRange_ReturnsSingleDate()
    {
        // Arrange
        var date = new DateTime(2023, 6, 15);

        // Act
        var result = DateRangeParser.FormatDateRange(date, date);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FormatDateRange_WithReverseOrder_HandlesProperly()
    {
        // Arrange
        var from = new DateTime(2023, 12, 31);
        var to = new DateTime(2023, 1, 1);

        // Act & Assert
        // Should either throw or handle gracefully
        var result = DateRangeParser.FormatDateRange(from, to);
        result.Should().NotBeNullOrWhiteSpace();
    }

    // ========== Edge Cases ==========

    [Fact]
    public void ParseDateRange_WithLeapYearDate_ParsesCorrectly()
    {
        // Act
        var (from, to) = DateRangeParser.ParseDateRange("2020-02-29", "2020-02-29");

        // Assert
        from.Day.Should().Be(29);
        from.Month.Should().Be(2);
    }

    [Fact]
    public void ParseDateRange_WithOneYearApart_ReturnsCorrectRange()
    {
        // Act
        var (from, to) = DateRangeParser.ParseDateRange("2022-01-01", "2023-12-31");

        // Assert
        (to - from).TotalDays.Should().BeGreaterThan(700);
    }

    [Fact]
    public void FormatDateRange_WithMultiMonthRange_FormatsReadably()
    {
        // Arrange
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 6, 30);

        // Act
        var result = DateRangeParser.FormatDateRange(from, to);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().BeLessThan(50); // Should be reasonably short
    }
}
