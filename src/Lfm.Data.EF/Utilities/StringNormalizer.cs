namespace Lfm.Data.EF.Utilities;

/// <summary>
/// Normalizes strings for consistent querying across Last.fm API and local data sources.
/// Handles common issues like apostrophe variants, spacing, and casing.
/// </summary>
public static class StringNormalizer
{
    /// <summary>
    /// Normalizes apostrophes to the standard ASCII apostrophe (U+0027).
    /// Last.fm APIs return regular apostrophes but scrobbles may contain smart quotes.
    /// </summary>
    /// <param name="value">String to normalize</param>
    /// <returns>String with all apostrophes normalized to U+0027</returns>
    public static string NormalizeApostrophes(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return value
            .Replace('\u2018', '\'')  // LEFT SINGLE QUOTATION MARK  → '
            .Replace('\u2019', '\'')  // RIGHT SINGLE QUOTATION MARK → '
            .Replace('\u201B', '\''); // SINGLE HIGH-REVERSED-9 QUOTATION MARK → '
    }

    /// <summary>
    /// Normalizes track or album name for comparison.
    /// Handles apostrophes and trims whitespace.
    /// </summary>
    /// <param name="value">Track or album name</param>
    /// <returns>Normalized name ready for comparison</returns>
    public static string NormalizeTrackName(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return NormalizeApostrophes(value.Trim());
    }

    /// <summary>
    /// Normalizes artist name for comparison.
    /// Handles apostrophes, trims whitespace, and normalizes "The" prefix.
    /// Note: Last.fm's autocorrect=1 already handles "The" prefix, so we just normalize apostrophes.
    /// </summary>
    /// <param name="value">Artist name</param>
    /// <returns>Normalized artist name ready for comparison</returns>
    public static string NormalizeArtistName(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        // Last.fm autocorrect handles "The" prefix ("Cure" → "The Cure")
        // We only need to normalize apostrophes
        return NormalizeApostrophes(value.Trim());
    }

    /// <summary>
    /// Normalizes album name for comparison.
    /// Handles apostrophes and trims whitespace.
    /// </summary>
    /// <param name="value">Album name</param>
    /// <returns>Normalized album name ready for comparison</returns>
    public static string NormalizeAlbumName(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        return NormalizeApostrophes(value.Trim());
    }
}
