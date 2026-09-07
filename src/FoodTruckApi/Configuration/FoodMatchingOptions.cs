using System.ComponentModel.DataAnnotations;

namespace FoodTruckApi.Configuration;

/// <summary>
/// Tuning for food matching, bound from the <c>FoodMatching</c> configuration section and
/// validated at startup.
/// </summary>
public sealed class FoodMatchingOptions
{
    public const string SectionName = "FoodMatching";

    /// <summary>
    /// Minimum match score (0..1) for a truck to be included when a food preference is
    /// supplied. Higher is stricter. Scores come from <c>LexicalFoodMatcher</c>: an exact
    /// stem match is 1.0 and fuzzy matches scale from roughly 0.55 upward, so values
    /// between about 0.55 and 1.0 are the useful tuning range.
    /// </summary>
    [Range(0d, 1d)]
    public double MatchThreshold { get; init; } = 0.7d;
}
