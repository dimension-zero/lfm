using FluentAssertions;
using Lfm.Core.Services;
using Lfm.Shared.Models;
using Lfm.Shared.Models.Results;
using Moq;
using Xunit;

namespace Lfm.Tests.Unit;

/// <summary>
/// Unit tests for DisplayService
/// Tests console output formatting for artists, tracks, albums, errors, timing, search, and recommendations
/// </summary>
public class DisplayServiceTests
{
    private readonly Mock<ISymbolProvider> _symbolsMock;
    private readonly DisplayService _service;

    public DisplayServiceTests()
    {
        _symbolsMock = new Mock<ISymbolProvider>();
        _symbolsMock.Setup(s => s.Error).Returns("❌");
        _symbolsMock.Setup(s => s.Success).Returns("✅");
        _symbolsMock.Setup(s => s.Tip).Returns("💡");
        _symbolsMock.Setup(s => s.Timer).Returns("⏱️");
        _symbolsMock.Setup(s => s.Stats).Returns("📊");
        _symbolsMock.Setup(s => s.Music).Returns("🎵");
        _symbolsMock.Setup(s => s.Clipboard).Returns("📋");
        _symbolsMock.Setup(s => s.StopSign).Returns("🛑");
        _symbolsMock.Setup(s => s.Settings).Returns("⚙️");
        _symbolsMock.Setup(s => s.Cleanup).Returns("🧹");

        _service = new DisplayService();
    }

    private static Artist CreateArtist(string name = "Test Artist", string playCount = "100")
    {
        return new Artist { Name = name, PlayCount = playCount };
    }

    private static Track CreateTrack(string name = "Test Track", string playCount = "50", string artistName = "Test Artist")
    {
        return new Track
        {
            Name = name,
            PlayCount = playCount,
            Artist = new ArtistInfo { Name = artistName }
        };
    }

    private static Album CreateAlbum(string name = "Test Album", string playCount = "25", string artistName = "Test Artist")
    {
        return new Album
        {
            Name = name,
            PlayCount = playCount,
            Artist = new ArtistInfo { Name = artistName }
        };
    }

    private static RecommendationResult CreateRecommendation(string artistName = "Recommended Artist", int sourceCount = 3)
    {
        return new RecommendationResult
        {
            ArtistName = artistName,
            Score = 85.5f,
            AverageSimilarity = 0.85f,
            OccurrenceCount = sourceCount,
            UserPlayCount = 0,
            SourceArtists = Enumerable.Range(1, sourceCount).Select(i => $"Source Artist {i}").ToList(),
            TopTracks = new List<Track>
            {
                CreateTrack("Track 1", "100"),
                CreateTrack("Track 2", "80")
            }
        };
    }

    // ========== DisplayArtists Tests ==========

    [Fact]
    public void DisplayArtists_WithSingleArtist_WritesFormattedOutput()
    {
        // Arrange
        var artists = new List<Artist> { CreateArtist("Pink Floyd", "1000") };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayArtists(artists, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Rank");
            output.Should().Contain("Artist");
            output.Should().Contain("Plays");
            output.Should().Contain("Pink Floyd");
            output.Should().Contain("1,000");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayArtists_WithMultipleArtists_WritesAllArtists()
    {
        // Arrange
        var artists = new List<Artist>
        {
            CreateArtist("Artist 1", "100"),
            CreateArtist("Artist 2", "200"),
            CreateArtist("Artist 3", "300")
        };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayArtists(artists, startRank: 5);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Artist 1");
            output.Should().Contain("Artist 2");
            output.Should().Contain("Artist 3");
            output.Should().Contain("5"); // Start rank
            output.Should().Contain("7"); // Last rank (5 + 2)
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayArtists_WithEmptyList_WritesHeaderOnly()
    {
        // Arrange
        var artists = new List<Artist>();
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayArtists(artists, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Rank");
            output.Should().Contain("Artist");
            output.Should().Contain("Plays");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayTracksForUser Tests ==========

    [Fact]
    public void DisplayTracksForUser_WithSingleTrack_WritesFormattedOutput()
    {
        // Arrange
        var tracks = new List<Track> { CreateTrack("Song", "50", "Pink Floyd") };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTracksForUser(tracks, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Rank");
            output.Should().Contain("Track");
            output.Should().Contain("Artist");
            output.Should().Contain("Plays");
            output.Should().Contain("Song");
            output.Should().Contain("Pink Floyd");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayTracksForUser_WithMultipleTracks_WritesAllTracks()
    {
        // Arrange
        var tracks = new List<Track>
        {
            CreateTrack("Track 1", "100"),
            CreateTrack("Track 2", "200"),
            CreateTrack("Track 3", "300")
        };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTracksForUser(tracks, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Track 1");
            output.Should().Contain("Track 2");
            output.Should().Contain("Track 3");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayTracksForArtist Tests ==========

    [Fact]
    public void DisplayTracksForArtist_WithSingleTrack_WritesFormattedOutput()
    {
        // Arrange
        var tracks = new List<Track> { CreateTrack("Song") };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTracksForArtist(tracks, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Rank");
            output.Should().Contain("Track");
            output.Should().Contain("Artist");
            output.Should().Contain("Song");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayAlbums Tests ==========

    [Fact]
    public void DisplayAlbums_WithSingleAlbum_WritesFormattedOutput()
    {
        // Arrange
        var albums = new List<Album> { CreateAlbum("Album Name", "75") };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayAlbums(albums, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Rank");
            output.Should().Contain("Album");
            output.Should().Contain("Artist");
            output.Should().Contain("Plays");
            output.Should().Contain("Album Name");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayRangeInfo Tests ==========

    [Fact]
    public void DisplayRangeInfo_WithVerboseTrue_WritesRangeInfo()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRangeInfo("artists", startIndex: 1, endIndex: 10, actualCount: 10, total: "500", verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Showing");
            output.Should().Contain("artists");
            output.Should().Contain("1");
            output.Should().Contain("500");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRangeInfo_WithVerboseFalse_WritesNothing()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRangeInfo("artists", startIndex: 1, endIndex: 10, actualCount: 10, total: "500", verbose: false);
            var output = stringWriter.ToString();

            // Assert
            output.Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayTotalInfo Tests ==========

    [Fact]
    public void DisplayTotalInfo_WithVerboseTrue_WritesTotalInfo()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTotalInfo("tracks", "1,234", verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Total");
            output.Should().Contain("tracks");
            output.Should().Contain("1,234");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayTotalInfo_WithVerboseFalse_WritesNothing()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTotalInfo("tracks", "1,234", verbose: false);
            var output = stringWriter.ToString();

            // Assert
            output.Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayOperationStart Tests ==========

    [Fact]
    public void DisplayOperationStart_WithArtistsAndRange_WritesRangeMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayOperationStart("artists", "testuser", "overall", startIndex: 1, endIndex: 50, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Getting artists");
            output.Should().Contain("1-50");
            output.Should().Contain("testuser");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayOperationStart_WithTracksAndLimit_WritesLimitMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayOperationStart("tracks", "testuser", "7day", limit: 50, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Getting top");
            output.Should().Contain("tracks");
            output.Should().Contain("50");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayOperationStart_WithVerboseFalse_WritesNothing()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayOperationStart("artists", "testuser", "overall", limit: 50, verbose: false);
            var output = stringWriter.ToString();

            // Assert
            output.Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayAnalysisProgress Tests ==========

    [Fact]
    public void DisplayAnalysisProgress_WithVerboseTrue_WritesProgressMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayAnalysisProgress("artists", analyzed: 100, found: 15, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Analyzed");
            output.Should().Contain("100");
            output.Should().Contain("found");
            output.Should().Contain("15");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayAnalysisProgress_WithMaxItems_IncludesMaxInMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayAnalysisProgress("tracks", analyzed: 50, found: 5, maxItems: 10, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("max:");
            output.Should().Contain("10");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayOperationComplete Tests ==========

    [Fact]
    public void DisplayOperationComplete_WithVerboseTrue_WritesCompleteMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayOperationComplete("artists", totalSearched: 500, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Search complete");
            output.Should().Contain("500");
            output.Should().Contain("artists");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayError (string) Tests ==========

    [Fact]
    public void DisplayError_WithMessage_WritesMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayError("Test error message");
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Test error message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayValidationError Tests ==========

    [Fact]
    public void DisplayValidationError_WithMessage_WritesMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayValidationError("Invalid input");
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Invalid input");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayWarning Tests ==========

    [Fact]
    public void DisplayWarning_WithMessage_WritesMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayWarning("Warning message");
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Warning message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplaySuccess Tests ==========

    [Fact]
    public void DisplaySuccess_WithMessage_WritesMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySuccess("Success message");
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Success message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayError (ErrorResult) Tests ==========

    [Fact]
    public void DisplayError_WithErrorResult_WritesMessageAndSymbol()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ApiError, "API failed");
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayError(error, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("API failed");
            output.Should().Contain("🌐"); // ApiError uses globe symbol
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayError_WithTechnicalDetails_WritesDetails()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ApiError, "API failed", "Connection timeout");
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayError(error, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Technical details");
            output.Should().Contain("Connection timeout");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayError_WithConfigurationError_WritesTip()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ConfigurationError, "Missing API key");
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayError(error, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Missing API key");
            output.Should().Contain("💡"); // Tip symbol
            output.Should().Contain("api-key");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayResult<T> Tests ==========

    [Fact]
    public void DisplayResult_WithSuccess_CallsOnSuccess()
    {
        // Arrange
        var result = Result<string>.Ok("test data");
        var callCount = 0;

        // Act
        _service.DisplayResult(result, _symbolsMock.Object, data =>
        {
            callCount++;
            data.Should().Be("test data");
        });

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void DisplayResult_WithError_DisplaysError()
    {
        // Arrange
        var error = new ErrorResult(ErrorType.ApiError, "Failed");
        var result = Result<string>.Fail(error);
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayResult(result, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Failed");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayResult (non-generic) Tests ==========

    [Fact]
    public void DisplayResult_NonGeneric_WithSuccess_CallsOnSuccess()
    {
        // Arrange
        var result = Result.Ok();
        var callCount = 0;

        // Act
        _service.DisplayResult(result, _symbolsMock.Object, () => callCount++);

        // Assert
        callCount.Should().Be(1);
    }

    // ========== DisplayTimingResults Tests ==========

    [Fact]
    public void DisplayTimingResults_WithMultipleResults_DisplaysTable()
    {
        // Arrange
        var timings = new List<TimingResult>
        {
            new() { Method = "GetArtists", CacheHit = true, ElapsedMs = 50, Details = "Cached" },
            new() { Method = "GetTracks", CacheHit = false, ElapsedMs = 200, Details = "API call" }
        };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTimingResults(timings, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("API Timing Results");
            output.Should().Contain("GetArtists");
            output.Should().Contain("HIT");
            output.Should().Contain("GetTracks");
            output.Should().Contain("MISS");
            output.Should().Contain("cache hits");
            output.Should().Contain("50.0%"); // Percentage format with 1 decimal place
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayTimingResults_WithEmptyList_WritesNothing()
    {
        // Arrange
        var timings = new List<TimingResult>();
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayTimingResults(timings, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().BeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayExecutionTime Tests ==========

    [Fact]
    public void DisplayExecutionTime_WritesExecutionTime()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayExecutionTime(1500, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Total execution time");
            output.Should().Contain("1500ms");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplaySearchStart Tests ==========

    [Fact]
    public void DisplaySearchStart_WithUnlimitedSearch_WritesUnlimitedMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySearchStart("tracks", "Pink Floyd", unlimited: true, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Getting your top");
            output.Should().Contain("tracks");
            output.Should().Contain("Unlimited");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplaySearchStart_WithLimitedSearch_WritesLimitMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySearchStart("albums", "The Beatles", unlimited: false, maxItems: 100, verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Getting your top");
            output.Should().Contain("albums");
            output.Should().Contain("100");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplaySearchCancelled Tests ==========

    [Fact]
    public void DisplaySearchCancelled_WithUserCancellation_WritesCancelledMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySearchCancelled(userCancelled: true, timeoutSeconds: 30, itemsSearched: 500, matchesFound: 25, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("cancelled by user");
            output.Should().Contain("500");
            output.Should().Contain("25");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplaySearchCancelled_WithTimeout_WritesTimeoutMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySearchCancelled(userCancelled: false, timeoutSeconds: 30, itemsSearched: 500, matchesFound: 25, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("timed out");
            output.Should().Contain("30");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplaySearchProgress Tests ==========

    [Fact]
    public void DisplaySearchProgress_WithVerboseTrue_WritesProgressMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplaySearchProgress(searched: 250, found: 15, itemType: "artists", verbose: true);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Searched");
            output.Should().Contain("250");
            output.Should().Contain("found");
            output.Should().Contain("15");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== DisplayRecommendations Tests ==========

    [Fact]
    public void DisplayRecommendations_WithPlaylistMode_WritesPlaylistHeader()
    {
        // Arrange
        var recommendations = new List<RecommendationResult> { CreateRecommendation() };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 3, verbose: false, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Playlist");
            output.Should().Contain("3 tracks each");
            output.Should().Contain("filter: >= 10");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRecommendations_WithoutPlaylistMode_WritesRecommendationsHeader()
    {
        // Arrange
        var recommendations = new List<RecommendationResult> { CreateRecommendation() };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 0, verbose: false, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Top");
            output.Should().Contain("Recommendations");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRecommendations_WithMultipleSourceArtists_ShowsCorrectFormat()
    {
        // Arrange
        var recommendations = new List<RecommendationResult>
        {
            CreateRecommendation(sourceCount: 3)
        };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 0, verbose: false, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Similar to: ");
            output.Should().Contain("Source Artist");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRecommendations_WithLargeSourceArtistList_ShowsFirstFiveAndMore()
    {
        // Arrange
        var recommendation = new RecommendationResult
        {
            ArtistName = "Test",
            SourceArtists = Enumerable.Range(1, 10).Select(i => $"Artist {i}").ToList()
        };
        var recommendations = new List<RecommendationResult> { recommendation };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 0, verbose: false, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("and 5 more");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRecommendations_WithVerboseMode_ShowsAllSourceArtists()
    {
        // Arrange
        var recommendation = new RecommendationResult
        {
            ArtistName = "Test",
            SourceArtists = Enumerable.Range(1, 10).Select(i => $"Artist {i}").ToList()
        };
        var recommendations = new List<RecommendationResult> { recommendation };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 0, verbose: true, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Artist 1");
            output.Should().Contain("Artist 10");
            output.Should().NotContain("and");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayRecommendations_WithTracks_DisplaysTrackList()
    {
        // Arrange
        var recommendations = new List<RecommendationResult> { CreateRecommendation() };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayRecommendations(recommendations, filter: 10, tracksPerArtist: 2, verbose: false, _symbolsMock.Object);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("Top tracks:");
            output.Should().Contain("Track 1");
            output.Should().Contain("Track 2");
            output.Should().Contain("Playlist Summary");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // ========== TruncateString Tests (via indirect testing through display methods) ==========

    [Fact]
    public void DisplayArtists_WithLongName_TruncatesName()
    {
        // Arrange
        var longName = new string('A', 50); // Longer than ArtistNameMaxLength (40)
        var artists = new List<Artist> { CreateArtist(longName) };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayArtists(artists, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain("...");
            output.Should().NotContain(longName); // Full name should not appear
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DisplayArtists_WithNameAtMaxLength_DoesNotTruncate()
    {
        // Arrange
        var name = new string('A', 40); // Exactly ArtistNameMaxLength
        var artists = new List<Artist> { CreateArtist(name) };
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(stringWriter);

            // Act
            _service.DisplayArtists(artists, startRank: 1);
            var output = stringWriter.ToString();

            // Assert
            output.Should().Contain(name);
            output.Should().NotContain("...");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
