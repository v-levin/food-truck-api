using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.Abstractions;

/// <summary>
/// Scores how well a truck's food offering matches a free-text preference. The score is
/// the mechanism; deciding what counts as a match (the threshold) is the caller's policy.
/// </summary>
public interface IFoodMatcher
{
    /// <summary>
    /// Prepares a scorer for one preference string (any per-query work, such as parsing
    /// the query into terms, happens once here), then returns a function that scores each
    /// truck against it on a 0..1 scale where 1 is an exact match.
    /// </summary>
    Func<FoodTruck, double> ForQuery(string foodQuery);
}
