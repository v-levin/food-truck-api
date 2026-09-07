using System.ComponentModel.DataAnnotations;

namespace FoodTruckApi.Configuration;

/// <summary>
/// Tunable limits for the food truck search, bound from the <c>FoodTruckSearch</c>
/// configuration section and validated at startup.
/// </summary>
public sealed class FoodTruckSearchOptions : IValidatableObject
{
    public const string SectionName = "FoodTruckSearch";

    /// <summary>How many trucks to return when the caller omits <c>amountOfResults</c>.</summary>
    [Range(1, int.MaxValue)]
    public int DefaultAmountOfResults { get; init; } = 10;

    /// <summary>The largest <c>amountOfResults</c> a caller is allowed to request.</summary>
    [Range(1, int.MaxValue)]
    public int MaxAmountOfResults { get; init; } = 50;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultAmountOfResults > MaxAmountOfResults)
        {
            yield return new ValidationResult(
                $"{nameof(DefaultAmountOfResults)} ({DefaultAmountOfResults}) must not exceed " +
                $"{nameof(MaxAmountOfResults)} ({MaxAmountOfResults}).",
                new[] { nameof(DefaultAmountOfResults) });
        }
    }
}
