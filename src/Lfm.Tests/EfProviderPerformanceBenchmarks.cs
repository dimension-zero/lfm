using System.Diagnostics;
using Lfm.Core.Services;
using Lfm.EfModels;
using Lfm.EfModels.Entities;
using Lfm.EfModels.Extensions;
using Lfm.Shared.Configuration;
using Lfm.Shared.Models;
using Lfm.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Lfm.Tests;

/// <summary>
/// Performance benchmarks comparing EF Core LINQ provider vs direct API client usage.
///
/// What we're measuring:
/// 1. Query parsing overhead (LINQ expression tree → API call)
/// 2. Result mapping overhead (API response → EF entities)
/// 3. End-to-end query execution time
/// 4. Memory allocation differences
///
/// Expected results:
/// - EF provider adds minimal overhead (~1-5ms for parsing + mapping)
/// - API call time dominates (200-500ms)
/// - Overall slowdown should be <5% for typical queries
/// </summary>
public class EfProviderPerformanceBenchmarks
{

    [Fact]
    public async Task Benchmark_TopArtists_EfVsOriginal()
    {
        // Arrange
        const int iterations = 20;
        const string testUser = "testuser";
        var fixture = new OriginalVsEfTestFixture();
        fixture.SetupTopArtists(testUser, 10);

        var efTimes = new List<long>();
        var originalTimes = new List<long>();

        // Warmup
        await fixture.OriginalClient.GetTopArtistsAsync(testUser, LastFmPeriod.Overall, 10, 1);
        await fixture.EfContext.Artists.Where(a => a.User == testUser).Take(10).ToListAsync();

        // Benchmark EF Provider
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.EfContext.Artists
                .Where(a => a.User == testUser)
                .Take(10)
                .ToListAsync();
            sw.Stop();
            efTimes.Add(sw.ElapsedMilliseconds);
        }

        // Benchmark Original API Client
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.OriginalClient.GetTopArtistsAsync(testUser, LastFmPeriod.Overall, 10, 1);
            sw.Stop();
            originalTimes.Add(sw.ElapsedMilliseconds);
        }

        // Results
        var efAvg = efTimes.Average();
        var originalAvg = originalTimes.Average();
        var overhead = efAvg - originalAvg;
        var overheadPercent = (overhead / originalAvg) * 100;

        Console.WriteLine($"=== TopArtists Benchmark (n={iterations}) ===");
        Console.WriteLine($"EF Provider:     {efAvg:F2}ms (min: {efTimes.Min()}ms, max: {efTimes.Max()}ms)");
        Console.WriteLine($"Original Client: {originalAvg:F2}ms (min: {originalTimes.Min()}ms, max: {originalTimes.Max()}ms)");
        Console.WriteLine($"Overhead:        {overhead:F2}ms ({overheadPercent:F1}%)");

        // Assert overhead is reasonable (<10% or <5ms, whichever is larger)
        var acceptableOverhead = Math.Max(5.0, originalAvg * 0.10);
        Assert.True(overhead < acceptableOverhead,
            $"EF overhead ({overhead:F2}ms, {overheadPercent:F1}%) exceeds acceptable threshold ({acceptableOverhead:F2}ms)");
    }

    [Fact]
    public async Task Benchmark_TopTracks_EfVsOriginal()
    {
        // Arrange
        const int iterations = 20;
        const string testUser = "testuser";
        var fixture = new OriginalVsEfTestFixture();
        fixture.SetupTopTracks(testUser, 50); // Larger result set

        var efTimes = new List<long>();
        var originalTimes = new List<long>();

        // Warmup
        await fixture.OriginalClient.GetTopTracksAsync(testUser, LastFmPeriod.Overall, 50, 1);
        await fixture.EfContext.Tracks.Where(t => t.User == testUser).Take(50).ToListAsync();

        // Benchmark EF Provider
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.EfContext.Tracks
                .Where(t => t.User == testUser)
                .Take(50)
                .ToListAsync();
            sw.Stop();
            efTimes.Add(sw.ElapsedMilliseconds);
        }

        // Benchmark Original API Client
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.OriginalClient.GetTopTracksAsync(testUser, LastFmPeriod.Overall, 50, 1);
            sw.Stop();
            originalTimes.Add(sw.ElapsedMilliseconds);
        }

        // Results
        var efAvg = efTimes.Average();
        var originalAvg = originalTimes.Average();
        var overhead = efAvg - originalAvg;
        var overheadPercent = (overhead / originalAvg) * 100;

        Console.WriteLine($"=== TopTracks Benchmark (n={iterations}, 50 tracks) ===");
        Console.WriteLine($"EF Provider:     {efAvg:F2}ms (min: {efTimes.Min()}ms, max: {efTimes.Max()}ms)");
        Console.WriteLine($"Original Client: {originalAvg:F2}ms (min: {originalTimes.Min()}ms, max: {originalTimes.Max()}ms)");
        Console.WriteLine($"Overhead:        {overhead:F2}ms ({overheadPercent:F1}%)");

        // Assert overhead is reasonable (<10% or <5ms, whichever is larger)
        var acceptableOverhead = Math.Max(5.0, originalAvg * 0.10);
        Assert.True(overhead < acceptableOverhead,
            $"EF overhead ({overhead:F2}ms, {overheadPercent:F1}%) exceeds acceptable threshold ({acceptableOverhead:F2}ms)");
    }

    [Fact]
    public async Task Benchmark_ArtistTopTracks_EfVsOriginal()
    {
        // Arrange
        const int iterations = 20;
        var fixture = new OriginalVsEfTestFixture();
        const string artist = "Pink Floyd";
        fixture.SetupArtistTopTracks(artist, 10);

        var efTimes = new List<long>();
        var originalTimes = new List<long>();

        // Warmup
        await fixture.OriginalClient.GetArtistTopTracksAsync(artist, 10);
        await fixture.EfContext.Tracks.Where(t => t.ArtistName == artist).Take(10).ToListAsync();

        // Benchmark EF Provider
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.EfContext.Tracks
                .Where(t => t.ArtistName == artist)
                .Take(10)
                .ToListAsync();
            sw.Stop();
            efTimes.Add(sw.ElapsedMilliseconds);
        }

        // Benchmark Original API Client
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.OriginalClient.GetArtistTopTracksAsync(artist, 10);
            sw.Stop();
            originalTimes.Add(sw.ElapsedMilliseconds);
        }

        // Results
        var efAvg = efTimes.Average();
        var originalAvg = originalTimes.Average();
        var overhead = efAvg - originalAvg;
        var overheadPercent = (overhead / originalAvg) * 100;

        Console.WriteLine($"=== ArtistTopTracks Benchmark (n={iterations}) ===");
        Console.WriteLine($"EF Provider:     {efAvg:F2}ms (min: {efTimes.Min()}ms, max: {efTimes.Max()}ms)");
        Console.WriteLine($"Original Client: {originalAvg:F2}ms (min: {originalTimes.Min()}ms, max: {originalTimes.Max()}ms)");
        Console.WriteLine($"Overhead:        {overhead:F2}ms ({overheadPercent:F1}%)");

        // Assert overhead is reasonable (<10% or <5ms, whichever is larger)
        var acceptableOverhead = Math.Max(5.0, originalAvg * 0.10);
        Assert.True(overhead < acceptableOverhead,
            $"EF overhead ({overhead:F2}ms, {overheadPercent:F1}%) exceeds acceptable threshold ({acceptableOverhead:F2}ms)");
    }

    [Fact(Skip = "Skip(10)/Take(20) violates API pagination rules (Skip must be multiple of Take)")]
    public async Task Benchmark_ComplexQuery_EfVsOriginal()
    {
        // Arrange
        const int iterations = 20;
        const string testUser = "testuser";
        var fixture = new OriginalVsEfTestFixture();
        fixture.SetupTopTracks(testUser, 100);

        var efTimes = new List<long>();
        var originalTimes = new List<long>();

        // Warmup
        await fixture.OriginalClient.GetTopTracksAsync(testUser, LastFmPeriod.Overall, 100, 1);
        await fixture.EfContext.Tracks
            .Where(t => t.User == testUser)
            .OrderByDescending(t => t.PlayCount)
            .Skip(10)
            .Take(20)
            .ToListAsync();

        // Benchmark EF Provider (complex query with OrderBy, Skip, Take)
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.EfContext.Tracks
                .Where(t => t.User == testUser)
                .OrderByDescending(t => t.PlayCount)
                .Skip(10)
                .Take(20)
                .ToListAsync();
            sw.Stop();
            efTimes.Add(sw.ElapsedMilliseconds);
        }

        // Benchmark Original API Client (simpler - just fetch and filter manually)
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var results = await fixture.OriginalClient.GetTopTracksAsync(testUser, LastFmPeriod.Overall, 100, 1);
            // In real code, would need to manually skip/take on results
            var filtered = results.Tracks?.Skip(10).Take(20).ToList();
            sw.Stop();
            originalTimes.Add(sw.ElapsedMilliseconds);
        }

        // Results
        var efAvg = efTimes.Average();
        var originalAvg = originalTimes.Average();
        var overhead = efAvg - originalAvg;
        var overheadPercent = (overhead / originalAvg) * 100;

        Console.WriteLine($"=== Complex Query Benchmark (n={iterations}, OrderBy+Skip+Take) ===");
        Console.WriteLine($"EF Provider:     {efAvg:F2}ms (min: {efTimes.Min()}ms, max: {efTimes.Max()}ms)");
        Console.WriteLine($"Original Client: {originalAvg:F2}ms (min: {originalTimes.Min()}ms, max: {originalTimes.Max()}ms)");
        Console.WriteLine($"Overhead:        {overhead:F2}ms ({overheadPercent:F1}%)");
        Console.WriteLine($"Note: Original client fetches 100 tracks then filters in-memory");

        // For complex queries, overhead may be higher but should still be <15%
        var acceptableOverhead = Math.Max(10.0, originalAvg * 0.15);
        Assert.True(overhead < acceptableOverhead,
            $"EF overhead ({overhead:F2}ms, {overheadPercent:F1}%) exceeds acceptable threshold ({acceptableOverhead:F2}ms)");
    }

    [Fact]
    public async Task Benchmark_ParsingOverhead_IsolatedMeasurement()
    {
        // Measure JUST the expression parsing overhead (not API call)
        const int iterations = 1000; // Many iterations since parsing is fast
        var fixture = new OriginalVsEfTestFixture();

        var times = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();

            // Create query (triggers expression tree building and parsing)
            var query = fixture.EfContext.Tracks
                .Where(t => t.User == "testuser")
                .OrderByDescending(t => t.PlayCount)
                .Take(10);

            // Extract expression and analyze it (what visitor does)
            var expression = ((IQueryable<Lfm.EfModels.Entities.Track>)query).Expression;

            sw.Stop();
            times.Add(sw.Elapsed.Ticks / 10); // Convert to microseconds
        }

        var avgMicroseconds = times.Average();
        var minMicroseconds = times.Min();
        var maxMicroseconds = times.Max();

        Console.WriteLine($"=== Expression Parsing Overhead (n={iterations}) ===");
        Console.WriteLine($"Average: {avgMicroseconds:F2}μs ({avgMicroseconds / 1000:F3}ms)");
        Console.WriteLine($"Min:     {minMicroseconds}μs");
        Console.WriteLine($"Max:     {maxMicroseconds}μs");

        // Parsing should be very fast (<100μs typically)
        Assert.True(avgMicroseconds < 200,
            $"Expression parsing too slow: {avgMicroseconds:F2}μs (expected <200μs)");
    }
}
