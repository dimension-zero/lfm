using FluentAssertions;
using Lfm.Core.Services;

namespace Lfm.Tests.Unit.Services;

/// <summary>
/// Unit tests for DateRangeParser - date parsing logic with no external dependencies
/// </summary>
[Trait("Category", "Unit")]
public class DateRangeParserTests
{
    #region ParseDateRange Tests

    [Fact]
    public void ParseDateRange_WithValidDates_ReturnsCorrectRange()
    {
        // Arrange
        var from = "2017-01-01";
        var to = "2017-12-31";

        // Act
        var (fromDate, toDate) = DateRangeParser.ParseDateRange(from, to);

        // Assert
        fromDate.Year.Should().Be(2017);
        fromDate.Month.Should().Be(1);
        fromDate.Day.Should().Be(1);
        toDate.Year.Should().Be(2017);
        toDate.Month.Should().Be(12);
        toDate.Day.Should().Be(31);
    }

    [Fact]
    public void ParseDateRange_WithYearShortcuts_ReturnsFullYearRange()
    {
        // Arrange
        var from = "2017";
        var to = "2018";

        // Act
        var (fromDate, toDate) = DateRangeParser.ParseDateRange(from, to);

        // Assert
        fromDate.Should().Be(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        toDate.Should().Be(new DateTime(2018, 12, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public void ParseDateRange_WithMissingFromDate_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseDateRange(null, "2017-12-31");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Both --from and --to must be specified*");
    }

    [Fact]
    public void ParseDateRange_WithMissingToDate_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseDateRange("2017-01-01", null);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Both --from and --to must be specified*");
    }

    [Fact]
    public void ParseDateRange_WithFromDateAfterToDate_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseDateRange("2018-01-01", "2017-12-31");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*From date must be before to date*");
    }

    #endregion

    #region ParseYearRange Tests

    [Fact]
    public void ParseYearRange_WithValidYear_ReturnsFullYearRange()
    {
        // Act
        var (fromDate, toDate) = DateRangeParser.ParseYearRange("2017");

        // Assert
        fromDate.Should().Be(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        toDate.Should().Be(new DateTime(2017, 12, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public void ParseYearRange_WithInvalidFormat_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseYearRange("not-a-year");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid year format*");
    }

    [Fact]
    public void ParseYearRange_WithYearTooOld_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseYearRange("1899");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*out of valid range*");
    }

    [Fact]
    public void ParseYearRange_WithYearTooFarInFuture_ThrowsArgumentException()
    {
        // Arrange
        var futureYear = (DateTime.Now.Year + 2).ToString();

        // Act
        Action act = () => DateRangeParser.ParseYearRange(futureYear);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*out of valid range*");
    }

    #endregion

    #region ParseSingleDate Tests

    [Fact]
    public void ParseSingleDate_WithYearShortcut_AsStartDate_ReturnsJanuary1st()
    {
        // Act
        var date = DateRangeParser.ParseSingleDate("2017", isStartDate: true);

        // Assert
        date.Should().Be(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ParseSingleDate_WithYearShortcut_AsEndDate_ReturnsDecember31st()
    {
        // Act
        var date = DateRangeParser.ParseSingleDate("2017", isStartDate: false);

        // Assert
        date.Should().Be(new DateTime(2017, 12, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public void ParseSingleDate_WithFullDate_AsEndDate_ReturnsEndOfDay()
    {
        // Act
        var date = DateRangeParser.ParseSingleDate("2017-06-15", isStartDate: false);

        // Assert
        date.Year.Should().Be(2017);
        date.Month.Should().Be(6);
        date.Day.Should().Be(15);
        date.Hour.Should().Be(23);
        date.Minute.Should().Be(59);
        date.Second.Should().Be(59);
    }

    [Fact]
    public void ParseSingleDate_WithInvalidFormat_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ParseSingleDate("not-a-date", isStartDate: true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid date format*");
    }

    #endregion

    #region ValidateDateRangeParameters Tests

    [Fact]
    public void ValidateDateRangeParameters_WithMultipleTimeParams_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ValidateDateRangeParameters(
            periodStr: "7day",
            fromStr: "2017-01-01",
            toStr: "2017-12-31",
            yearStr: null
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Cannot specify multiple time parameters*");
    }

    [Fact]
    public void ValidateDateRangeParameters_WithOnlyFromDate_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ValidateDateRangeParameters(
            periodStr: null,
            fromStr: "2017-01-01",
            toStr: null,
            yearStr: null
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*you must also specify --to*");
    }

    [Fact]
    public void ValidateDateRangeParameters_WithOnlyToDate_ThrowsArgumentException()
    {
        // Act
        Action act = () => DateRangeParser.ValidateDateRangeParameters(
            periodStr: null,
            fromStr: null,
            toStr: "2017-12-31",
            yearStr: null
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*you must also specify --from*");
    }

    #endregion

    #region ToUnixTimestamp Tests

    [Fact]
    public void ToUnixTimestamp_ConvertsDateTimeCorrectly()
    {
        // Arrange
        var date = new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var timestamp = DateRangeParser.ToUnixTimestamp(date);

        // Assert
        timestamp.Should().Be(1483228800); // Known Unix timestamp for 2017-01-01 00:00:00 UTC
    }

    #endregion
}
