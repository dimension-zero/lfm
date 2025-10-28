using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Lfm.EfModels.Utilities;

namespace Lfm.Tests.Benchmarks;

/// <summary>
/// Benchmarks for string normalization performance.
/// Demonstrates the overhead of apostrophe normalization vs the cost of API retries.
///
/// Run with: dotnet run -c Release --project src/Lfm.Tests -- --benchmark
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StringNormalizationBenchmarks
{
    private readonly string[] _testStrings = new[]
    {
        "Don't Stop Me Now",          // Standard apostrophe (U+0027)
        "Don't Stop Believin'",       // Left single quotation (U+2018)
        "Can't Buy Me Love",          // Right single quotation (U+2019)
        "It's Only Rock 'n' Roll",    // Multiple apostrophes
        "Wouldn't It Be Nice",        // Standard apostrophe
        "'Tis the Damn Season",       // Starts with apostrophe
        "L'Amour Toujours",           // French apostrophe
        "Mama's Broken Heart",        // Possessive
        "Smokin' in the Boys Room",   // Dropping g
        "Sweet Child O' Mine"         // Contraction
    };

    #region Single String Operations

    [Benchmark(Baseline = true)]
    public string NormalizeTrackName_SingleString()
    {
        return StringNormalizer.NormalizeTrackName("Don't Stop Me Now");
    }

    [Benchmark]
    public string NormalizeTrackName_WithSmartQuote()
    {
        return StringNormalizer.NormalizeTrackName("Don't Stop Me Now");
    }

    [Benchmark]
    public string NormalizeArtistName_SingleString()
    {
        return StringNormalizer.NormalizeArtistName("Guns N' Roses");
    }

    [Benchmark]
    public string NormalizeAlbumName_SingleString()
    {
        return StringNormalizer.NormalizeAlbumName("Let's Dance");
    }

    #endregion

    #region Batch Operations

    [Benchmark]
    public List<string> NormalizeBatch_10Tracks()
    {
        var results = new List<string>(10);
        for (int i = 0; i < 10; i++)
        {
            results.Add(StringNormalizer.NormalizeTrackName(_testStrings[i % _testStrings.Length]));
        }
        return results;
    }

    [Benchmark]
    public List<string> NormalizeBatch_100Tracks()
    {
        var results = new List<string>(100);
        for (int i = 0; i < 100; i++)
        {
            results.Add(StringNormalizer.NormalizeTrackName(_testStrings[i % _testStrings.Length]));
        }
        return results;
    }

    [Benchmark]
    public List<string> NormalizeBatch_1000Tracks()
    {
        var results = new List<string>(1000);
        for (int i = 0; i < 1000; i++)
        {
            results.Add(StringNormalizer.NormalizeTrackName(_testStrings[i % _testStrings.Length]));
        }
        return results;
    }

    #endregion

    #region Comparison: Normalization vs String Operations

    /// <summary>
    /// Baseline: Simple string trim operation
    /// </summary>
    [Benchmark]
    public string Baseline_StringTrim()
    {
        return "  Don't Stop Me Now  ".Trim();
    }

    /// <summary>
    /// Baseline: String replace operation (single character)
    /// </summary>
    [Benchmark]
    public string Baseline_StringReplace()
    {
        return "Don't Stop Me Now".Replace('\'', '\'');
    }

    /// <summary>
    /// Original approach simulation: Multiple string replacements (retry logic)
    /// </summary>
    [Benchmark]
    public List<string> OriginalApproach_ThreeReplacements()
    {
        var track = "Don't Stop Me Now";
        var results = new List<string>(3)
        {
            track,  // Original
            track.Replace('\'', '\u2018'),  // Left quote variant
            track.Replace('\'', '\u2019')   // Right quote variant
        };
        return results;
    }

    /// <summary>
    /// New approach: Single normalization call
    /// </summary>
    [Benchmark]
    public string NewApproach_SingleNormalization()
    {
        return StringNormalizer.NormalizeTrackName("Don't Stop Me Now");
    }

    #endregion

    #region Edge Cases

    [Benchmark]
    public string NormalizeNull()
    {
        return StringNormalizer.NormalizeTrackName(null);
    }

    [Benchmark]
    public string NormalizeEmpty()
    {
        return StringNormalizer.NormalizeTrackName("");
    }

    [Benchmark]
    public string NormalizeWhitespace()
    {
        return StringNormalizer.NormalizeTrackName("   ");
    }

    [Benchmark]
    public string NormalizeLongString()
    {
        var longString = string.Join(" ", Enumerable.Repeat("Don't Stop", 100));
        return StringNormalizer.NormalizeTrackName(longString);
    }

    #endregion

    #region Realistic Scenarios

    /// <summary>
    /// Simulates processing user's top 50 tracks (typical Last.fm query)
    /// </summary>
    [Benchmark]
    public List<(string track, string artist)> RealisticScenario_Top50Tracks()
    {
        var results = new List<(string, string)>(50);

        for (int i = 0; i < 50; i++)
        {
            var track = _testStrings[i % _testStrings.Length];
            var artist = i % 2 == 0 ? "Guns N' Roses" : "The Beatles";

            results.Add((
                StringNormalizer.NormalizeTrackName(track),
                StringNormalizer.NormalizeArtistName(artist)
            ));
        }

        return results;
    }

    /// <summary>
    /// Simulates processing 1000 scrobbles from local file
    /// </summary>
    [Benchmark]
    public List<(string track, string artist, string album)> RealisticScenario_1000Scrobbles()
    {
        var results = new List<(string, string, string)>(1000);

        for (int i = 0; i < 1000; i++)
        {
            var track = _testStrings[i % _testStrings.Length];
            var artist = i % 2 == 0 ? "Guns N' Roses" : "The Beatles";
            var album = i % 3 == 0 ? "Appetite for Destruction" : "Abbey Road";

            results.Add((
                StringNormalizer.NormalizeTrackName(track),
                StringNormalizer.NormalizeArtistName(artist),
                StringNormalizer.NormalizeAlbumName(album)
            ));
        }

        return results;
    }

    #endregion
}
