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
    // Below this fuzzy ratio the two tokens are treated as unrelated, so their score is 0.
    private const int FuzzyFloor = 70;

    public double Score(string foodQuery, FoodTruck truck)
    {
        var queryTerms = FoodTextNormalizer.ExtractQueryTerms(foodQuery);
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

        // One term contained in the other (e.g. "chicken" in "chickenwings").
        if (queryTerm.Length >= 3 &&
            (truckTerm.Contains(queryTerm, StringComparison.Ordinal) ||
             queryTerm.Contains(truckTerm, StringComparison.Ordinal)))
        {
            return 0.9d;
        }

        var ratio = Fuzz.WeightedRatio(queryTerm, truckTerm);
        return ratio < FuzzyFloor ? 0d : ratio / 100d;
    }
}
