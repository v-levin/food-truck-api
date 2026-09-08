using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;
using FuzzySharp;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// Lexical food matcher: reduces both sides with <see cref="FoodTextNormalizer"/> (so
/// "Tacos" matches "taco"), then falls back to fuzzy string similarity for typos and
/// near-variants ("burito" -> "burrito"). It has no concept of meaning, so "pho" will not
/// match "vietnamese soup" - that would need the semantic matcher we deferred.
/// </summary>
internal sealed class LexicalFoodMatcher : IFoodMatcher
{
    /// <summary>
    /// Terms shorter than this must match exactly. Short tokens collide far too easily
    /// under substring and fuzzy comparison (e.g. "ice" is a substring of "rice", and
    /// three of four characters match), which produced confident matches for unrelated
    /// foods.
    /// </summary>
    private const int MinComparableLength = 4;

    /// <summary>Fuzzy ratios below this mean "unrelated"; the score is 0.</summary>
    private const int FuzzyFloor = 55;

    public Func<FoodTruck, double> ForQuery(string foodQuery)
    {
        var queryTerms = FoodTextNormalizer.ExtractTerms(foodQuery);
        return truck => Score(queryTerms, truck);
    }

    private static double Score(IReadOnlyList<string> queryTerms, FoodTruck truck)
    {
        if (queryTerms.Count == 0 || truck.FoodTerms.Count == 0)
        {
            return 0d;
        }

        double best = 0d;

        foreach (var queryTerm in queryTerms)
        {
            foreach (var truckTerm in truck.FoodTerms)
            {
                best = Math.Max(best, TermScore(queryTerm, truckTerm));
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

        if (queryTerm.Length < MinComparableLength || truckTerm.Length < MinComparableLength)
        {
            return 0d;
        }

        // WeightedRatio already blends full and partial (substring) similarity, so it
        // covers typos ("burito"/"burrito") and roots inside compounds ("shake"/"milkshake").
        var ratio = Fuzz.WeightedRatio(queryTerm, truckTerm);
        return ratio < FuzzyFloor ? 0d : ratio / 100d;
    }
}
