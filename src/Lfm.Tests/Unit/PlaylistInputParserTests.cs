using FluentAssertions;
using Lfm.Core.Services;
using Lfm.Shared.Models;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for PlaylistInputParser
/// Tests CSV and JSON parsing, validation, and error handling
/// </summary>
public class PlaylistInputParserTests
{
    private readonly PlaylistInputParser _parser = new();

    // ========== CSV Parsing Tests ==========

    [Fact]
    public void ParseCommaSeparated_WithValidSingleTrack_ReturnsList()
    {
        // Arrange
        const string input = "Pink Floyd,Money";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data![0].Artist.Should().Be("Pink Floyd");
        result.Data[0].Track.Should().Be("Money");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ParseCommaSeparated_WithMultipleTracks_ReturnsList()
    {
        // Arrange
        const string input = "Pink Floyd,Money;The Beatles,Hey Jude;David Bowie,Space Oddity";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data![0].Artist.Should().Be("Pink Floyd");
        result.Data[1].Artist.Should().Be("The Beatles");
        result.Data[2].Artist.Should().Be("David Bowie");
    }

    [Fact]
    public void ParseCommaSeparated_WithWhitespace_TrimsValues()
    {
        // Arrange
        const string input = "  Pink Floyd  ,  Money  ";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data![0].Artist.Should().Be("Pink Floyd");
        result.Data[0].Track.Should().Be("Money");
    }

    [Fact]
    public void ParseCommaSeparated_WithEmptyString_ReturnsFail()
    {
        // Arrange
        const string input = "";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("Input cannot be empty");
    }

    [Fact]
    public void ParseCommaSeparated_WithWhitespaceOnly_ReturnsFail()
    {
        // Arrange
        const string input = "   \t  \n  ";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("Input cannot be empty");
    }

    [Fact]
    public void ParseCommaSeparated_WithMissingArtistName_ReturnsError()
    {
        // Arrange
        const string input = ",Money";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("entry 1");
        result.Errors[0].Should().Contain("Expected format");
    }

    [Fact]
    public void ParseCommaSeparated_WithMissingTrackName_ReturnsError()
    {
        // Arrange
        const string input = "Pink Floyd,";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("Expected format");
    }

    [Fact]
    public void ParseCommaSeparated_WithMalformedEntry_ReturnsError()
    {
        // Arrange
        const string input = "Pink Floyd";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("Expected format 'artist,track'");
    }

    [Fact]
    public void ParseCommaSeparated_WithMixedValidAndInvalid_ReturnsOnlyErrors()
    {
        // Arrange
        const string input = "Pink Floyd,Money;,Invalid;The Beatles,Hey Jude";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("entry 2");
        result.Data.Should().BeNull();
    }

    [Fact]
    public void ParseCommaSeparated_WithOversizedArtistName_ReturnsError()
    {
        // Arrange
        var longArtistName = new string('A', 201);
        var input = $"{longArtistName},Money";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("Artist name too long");
    }

    [Fact]
    public void ParseCommaSeparated_WithOversizedTrackName_ReturnsError()
    {
        // Arrange
        var longTrackName = new string('T', 201);
        var input = $"Pink Floyd,{longTrackName}";

        // Act
        var result = _parser.ParseCommaSeparated(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("Track name too long");
    }

    // ========== JSON Parsing Tests ==========

    [Fact]
    public void ParseJson_WithValidSingleTrack_ReturnsList()
    {
        // Arrange
        const string json = "[{\"artist\":\"Pink Floyd\",\"track\":\"Money\"}]";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Artist.Should().Be("Pink Floyd");
        result.Data[0].Track.Should().Be("Money");
    }

    [Fact]
    public void ParseJson_WithMultipleTracks_ReturnsList()
    {
        // Arrange
        const string json = """
            [
                {"artist":"Pink Floyd","track":"Money"},
                {"artist":"The Beatles","track":"Hey Jude"},
                {"artist":"David Bowie","track":"Space Oddity"}
            ]
            """;

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public void ParseJson_WithFormattedWhitespace_ParsesCorrectly()
    {
        // Arrange
        const string json = """
            [
                {
                    "artist": "Pink Floyd",
                    "track": "Money"
                },
                {
                    "artist": "The Beatles",
                    "track": "Hey Jude"
                }
            ]
            """;

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public void ParseJson_WithEmptyString_ReturnsFail()
    {
        // Arrange
        const string json = "";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("JSON input cannot be empty");
    }

    [Fact]
    public void ParseJson_WithEmptyArray_ReturnsFail()
    {
        // Arrange
        const string json = "[]";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("No tracks found in JSON");
    }

    [Fact]
    public void ParseJson_WithMalformedJson_ReturnsError()
    {
        // Arrange
        const string json = "{invalid json}";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].Should().Contain("Invalid JSON format");
    }

    [Fact]
    public void ParseJson_WithMissingArtist_ReturnsError()
    {
        // Arrange
        const string json = "[{\"track\":\"Money\"}]";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("track 1");
        result.Errors[0].Should().Contain("Artist name cannot be empty");
    }

    [Fact]
    public void ParseJson_WithMissingTrack_ReturnsError()
    {
        // Arrange
        const string json = "[{\"artist\":\"Pink Floyd\"}]";

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("Track name cannot be empty");
    }

    [Fact]
    public void ParseJson_WithMixedValidAndInvalid_ReturnsOnlyErrors()
    {
        // Arrange
        const string json = """
            [
                {"artist":"Pink Floyd","track":"Money"},
                {"artist":"","track":"Invalid"},
                {"artist":"The Beatles","track":"Hey Jude"}
            ]
            """;

        // Act
        var result = _parser.ParseJson(json);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Should().Contain("track 2");
        result.Data.Should().BeNull();
    }

    // ========== Track Validation Tests ==========

    [Fact]
    public void ValidateTrackRequest_WithValidTrack_ReturnsSuccess()
    {
        // Arrange
        var track = new TrackRequest("Pink Floyd", "Money");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateTrackRequest_WithEmptyArtist_ReturnsError()
    {
        // Arrange
        var track = new TrackRequest("", "Money");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Artist name cannot be empty");
    }

    [Fact]
    public void ValidateTrackRequest_WithEmptyTrack_ReturnsError()
    {
        // Arrange
        var track = new TrackRequest("Pink Floyd", "");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Track name cannot be empty");
    }

    [Fact]
    public void ValidateTrackRequest_WithBothEmpty_ReturnsMultipleErrors()
    {
        // Arrange
        var track = new TrackRequest("", "");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Artist name cannot be empty");
        result.Errors.Should().Contain("Track name cannot be empty");
    }

    [Fact]
    public void ValidateTrackRequest_WithLongArtistName_ReturnsError()
    {
        // Arrange
        var longName = new string('A', 201);
        var track = new TrackRequest(longName, "Money");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Artist name too long (max 200 characters)");
    }

    [Fact]
    public void ValidateTrackRequest_WithMaxLengthArtistName_Succeeds()
    {
        // Arrange
        var maxName = new string('A', 200);
        var track = new TrackRequest(maxName, "Money");

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTrackRequest_WithLongTrackName_ReturnsError()
    {
        // Arrange
        var longName = new string('T', 201);
        var track = new TrackRequest("Pink Floyd", longName);

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Track name too long (max 200 characters)");
    }

    [Fact]
    public void ValidateTrackRequest_WithMaxLengthTrackName_Succeeds()
    {
        // Arrange
        var maxName = new string('T', 200);
        var track = new TrackRequest("Pink Floyd", maxName);

        // Act
        var result = _parser.ValidateTrackRequest(track);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
