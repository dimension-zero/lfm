using Lfm.Data.EF.Utilities;
using Xunit;

namespace Lfm.Tests.Unit.Utilities;

/// <summary>
/// Unit tests for StringNormalizer utility.
/// Tests apostrophe normalization for consistent API querying.
/// </summary>
public class StringNormalizerTests
{
    [Theory]
    [InlineData("Don't Stop Me Now", "Don't Stop Me Now")] // Already standard apostrophe
    [InlineData("Don't Stop Me Now", "Don't Stop Me Now")] // Left single quotation mark (U+2018)
    [InlineData("Don't Stop Me Now", "Don't Stop Me Now")] // Right single quotation mark (U+2019)
    [InlineData("‛Don't Stop", "'Don't Stop")] // Single high-reversed-9 (U+201B)
    [InlineData("Can't Buy Me Love", "Can't Buy Me Love")] // Multiple apostrophes
    public void NormalizeApostrophes_ConvertsAllVariantsToStandardApostrophe(string input, string expected)
    {
        // Act
        var result = StringNormalizer.NormalizeApostrophes(input);

        // Assert
        Assert.Equal(expected, result);
        Assert.All(result, c => Assert.True(c != '\u2018' && c != '\u2019' && c != '\u201B'));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeApostrophes_HandlesNullAndEmpty(string? input, string expected)
    {
        // Act
        var result = StringNormalizer.NormalizeApostrophes(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  Sgt. Pepper's  ", "Sgt. Pepper's")] // Trim whitespace + normalize
    [InlineData("It's Only Rock 'n' Roll", "It's Only Rock 'n' Roll")] // Multiple variants
    public void NormalizeTrackName_TrimsAndNormalizesApostrophes(string input, string expected)
    {
        // Act
        var result = StringNormalizer.NormalizeTrackName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  The Beatles  ", "The Beatles")] // Trim whitespace
    [InlineData("Guns N' Roses", "Guns N' Roses")] // Normalize apostrophe
    public void NormalizeArtistName_TrimsAndNormalizesApostrophes(string input, string expected)
    {
        // Act
        var result = StringNormalizer.NormalizeArtistName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  Let's Dance  ", "Let's Dance")] // Trim whitespace + normalize
    [InlineData("They're Only Chasing Safety", "They're Only Chasing Safety")] // Smart quote
    public void NormalizeAlbumName_TrimsAndNormalizesApostrophes(string input, string expected)
    {
        // Act
        var result = StringNormalizer.NormalizeAlbumName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeApostrophes_PreservesOtherUnicodeCharacters()
    {
        // Arrange - string with various unicode characters that should NOT be normalized
        var input = "Café Müller – The Naïve's Song";

        // Act
        var result = StringNormalizer.NormalizeApostrophes(input);

        // Assert
        Assert.Contains("é", result); // Should preserve accented e
        Assert.Contains("ü", result); // Should preserve umlaut
        Assert.Contains("ï", result); // Should preserve diaeresis
        Assert.Contains("–", result); // Should preserve en-dash
        Assert.Contains("'", result); // Should normalize apostrophe to U+0027
    }

    [Fact]
    public void DocumentedUseCase_ApostropheInLastFmQuery()
    {
        // This demonstrates the original problem:
        // Last.fm returns "Don't Stop Me Now" with U+0027
        // But user's scrobbles might contain "Don't" with U+2019
        // Without normalization, these wouldn't match and would require 3 API calls to retry

        // Arrange - User's query from scrobble data (smart quote)
        var userQuery = "Don't Stop Me Now";

        // Last.fm API canonical form (standard apostrophe)
        var lastFmCanonical = "Don't Stop Me Now";

        // Act - Normalize user's query
        var normalizedQuery = StringNormalizer.NormalizeTrackName(userQuery);

        // Assert - After normalization, they match (single API call succeeds)
        Assert.Equal(lastFmCanonical, normalizedQuery);
    }
}
