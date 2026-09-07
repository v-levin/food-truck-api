using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.Abstractions;

/// <summary>
/// Scores how well a truck's food offering matches a free-text preference. The score is
/// the mechanism; deciding what counts as a match (the threshold) is the caller's policy.
/// </summary>
public interface IFoodMatcher
{
    /// <summary>
    /// Returns a score in the range 0..1, where 1 is an exact match and 0 is no match.
    /// </summary>
    double Score(string foodQuery, FoodTruck truck);
}
