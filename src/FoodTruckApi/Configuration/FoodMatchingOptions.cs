using System.ComponentModel.DataAnnotations;

namespace FoodTruckApi.Configuration;

/// <summary>
/// Tuning for food matching, bound from the <c>FoodMatching</c> configuration section and
/// validated at startup.
/// </summary>
public sealed class FoodMatchingOptions : IValidatableObject
{
    public const string SectionName = "FoodMatching";

    /// <summary>
    /// Minimum match score (0..1) for a truck to be included when a food preference is
    /// supplied. Higher is stricter. An exact stem match scores 1.0, an alias match 0.9,
    /// fuzzy matches scale from ~0.55, and semantic matches use the range below.
    /// </summary>
    [Range(0d, 1d)]
    public double MatchThreshold { get; init; } = 0.7d;

    /// <summary>Cosine similarity at or below this contributes no semantic score.</summary>
    [Range(0d, 1d)]
    public double SemanticFloor { get; init; } = 0.58d;

    /// <summary>Cosine similarity at or above this is treated as a full semantic match (1.0).</summary>
    [Range(0d, 1d)]
    public double SemanticStrong { get; init; } = 0.72d;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SemanticFloor >= SemanticStrong)
        {
            yield return new ValidationResult(
                $"{nameof(SemanticFloor)} ({SemanticFloor}) must be less than " +
                $"{nameof(SemanticStrong)} ({SemanticStrong}).",
                new[] { nameof(SemanticFloor) });
        }
    }
}
