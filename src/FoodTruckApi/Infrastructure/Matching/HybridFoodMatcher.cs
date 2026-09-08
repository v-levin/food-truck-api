using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FuzzySharp;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// Scores a food preference against a truck by taking the strongest of three signals:
/// <list type="number">
///   <item>a "serves everything" listing that doesn't exclude the query;</item>
///   <item>a lexical match on the listed foods — exact stem, then <see cref="FoodAliases"/>,
///         then fuzzy string similarity for typos;</item>
///   <item>semantic similarity between the query and the whole listing via the embedding model.</item>
/// </list>
/// </summary>
internal sealed class HybridFoodMatcher : IFoodMatcher
{
    // Non-exact terms shorter than this are too collision-prone to fuzzy-match ("ice" is
    // inside "rice"), so they must match exactly or via an alias.
    private const int MinComparableLength = 4;
    private const int FuzzyFloor = 55;
    private const double AliasScore = 0.9d;
    private const double CatchAllScore = 0.85d;

    private readonly IFoodEmbedder _embedder;
    private readonly FoodEmbeddingIndex _embeddingIndex;
    private readonly FoodMatchingOptions _options;

    public HybridFoodMatcher(
        IFoodEmbedder embedder,
        FoodEmbeddingIndex embeddingIndex,
        IOptions<FoodMatchingOptions> options)
    {
        _embedder = embedder;
        _embeddingIndex = embeddingIndex;
        _options = options.Value;
    }

    public Func<FoodTruck, double> ForQuery(string foodQuery)
    {
        var queryTerms = FoodTextNormalizer.ExtractTerms(foodQuery);
        var queryVector = queryTerms.Count > 0 ? _embedder.Embed(foodQuery) : null;

        return truck => Score(queryTerms, queryVector, truck);
    }

    private double Score(IReadOnlyList<string> queryTerms, float[]? queryVector, FoodTruck truck)
    {
        if (queryTerms.Count == 0)
        {
            return 0d;
        }

        var offering = truck.Offering;

        if (offering.ServesEverything && !queryTerms.Any(offering.Excludes.Contains))
        {
            return CatchAllScore;
        }

        var lexical = LexicalScore(queryTerms, offering.Terms);
        var semantic = SemanticScore(queryVector, _embeddingIndex.For(truck));

        return Math.Max(lexical, semantic);
    }

    private static double LexicalScore(IReadOnlyList<string> queryTerms, IReadOnlyList<string> truckTerms)
    {
        double best = 0d;

        foreach (var queryTerm in queryTerms)
        {
            foreach (var truckTerm in truckTerms)
            {
                var score = TermScore(queryTerm, truckTerm);
                if (score > best)
                {
                    best = score;
                }

                if (best >= 1d)
                {
                    return 1d;
                }
            }
        }

        return best;
    }

    private static double TermScore(string queryTerm, string truckTerm)
    {
        if (queryTerm == truckTerm)
        {
            return 1d;
        }

        if (FoodAliases.Related(queryTerm, truckTerm))
        {
            return AliasScore;
        }

        if (queryTerm.Length < MinComparableLength || truckTerm.Length < MinComparableLength)
        {
            return 0d;
        }

        var ratio = Fuzz.WeightedRatio(queryTerm, truckTerm);
        return ratio < FuzzyFloor ? 0d : ratio / 100d;
    }

    private double SemanticScore(float[]? query, float[]? truck)
    {
        if (query is null || truck is null)
        {
            return 0d;
        }

        var cosine = DotProduct(query, truck);

        if (cosine <= _options.SemanticFloor)
        {
            return 0d;
        }

        if (cosine >= _options.SemanticStrong)
        {
            return 1d;
        }

        return (cosine - _options.SemanticFloor) / (_options.SemanticStrong - _options.SemanticFloor);
    }

    private static double DotProduct(float[] a, float[] b)
    {
        double sum = 0d;
        for (var i = 0; i < a.Length; i++)
        {
            sum += a[i] * (double)b[i];
        }

        return sum;
    }
}
