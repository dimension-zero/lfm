using Lfm.Framework.Abstractions;
using Lfm.Framework.Models;
using Lfm.Wine.Models;
using Lfm.Wine.Data;
using Lfm.Shared.Models.Results;

namespace Lfm.Wine.Algorithms;

/// <summary>
/// Wine recommendation algorithm implementing IRecommendationAlgorithm<WineItem>
/// Recommends wines based on user's taste profile and variety preferences
/// </summary>
public class WineRecommendationAlgorithm : IRecommendationAlgorithm<WineItem>
{
    private readonly MockWineDataProvider _dataProvider;

    public WineRecommendationAlgorithm(MockWineDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Result<List<RecommendationResult<WineItem>>>> GenerateRecommendationsAsync(
        string userId, string period = "overall", int analysisLimit = 20, int recommendationLimit = 20, int filterThreshold = 0)
    {
        try
        {
            var userWines = _dataProvider.GetAllWines()
                .Where(w => w.UserTastingCount > filterThreshold)
                .OrderByDescending(w => w.UserRating ?? 0)
                .Take(analysisLimit)
                .ToList();

            if (!userWines.Any())
                return Result<List<RecommendationResult<WineItem>>>.Ok(new List<RecommendationResult<WineItem>>());

            // Extract varieties user enjoys
            var favoriteVarieties = userWines
                .Where(w => w.Variety != null)
                .Select(w => w.Variety!)
                .Distinct()
                .ToList();

            // Find similar wines not yet tasted
            var recommendations = new Dictionary<string, RecommendationResult<WineItem>>();

            foreach (var wine in userWines)
            {
                if (wine.Variety != null)
                {
                    var similar = _dataProvider.GetSimilarWines(wine.Variety, wine.Region)
                        .Where(w => w.Id != wine.Id && w.UserTastingCount == 0)
                        .Take(5)
                        .ToList();

                    foreach (var rec in similar)
                    {
                        var score = (wine.UserRating ?? 3.5f) * (rec.AverageRating ?? 3.5f) / 5f;

                        if (!recommendations.ContainsKey(rec.Id))
                        {
                            recommendations[rec.Id] = new RecommendationResult<WineItem>
                            {
                                Item = rec,
                                Score = score,
                                AverageRelevance = score,
                                OccurrenceCount = 1,
                                UserInteractionCount = 0,
                                SourceItems = new List<string> { wine.Name }
                            };
                        }
                        else
                        {
                            var existing = recommendations[rec.Id];
                            existing.Score += score;
                            existing.OccurrenceCount++;
                            existing.SourceItems.Add(wine.Name);
                            existing.AverageRelevance = existing.Score / existing.OccurrenceCount;
                        }
                    }
                }
            }

            var results = recommendations.Values
                .OrderByDescending(r => r.Score)
                .Take(recommendationLimit)
                .ToList();

            return await Task.FromResult(Result<List<RecommendationResult<WineItem>>>.Ok(results));
        }
        catch (Exception ex)
        {
            return Result<List<RecommendationResult<WineItem>>>.Fail(
                ErrorType.UnknownError, $"Failed to generate recommendations", ex.Message);
        }
    }

    public async Task<Result<List<RecommendationResult<WineItem>>>> GenerateRecommendationsForDateRangeAsync(
        string userId, DateTime from, DateTime to, int analysisLimit = 20, int recommendationLimit = 20, int filterThreshold = 0)
    {
        // For POC, same as overall recommendations
        return await GenerateRecommendationsAsync(userId, "overall", analysisLimit, recommendationLimit, filterThreshold);
    }
}
