using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.FindFoodTrucks;

/// <summary>
/// A validated request to find nearby food trucks. Construction implies the inputs have
/// already passed validation at the API boundary, so the handler can trust them.
/// </summary>
/// <param name="Origin">The location to search around.</param>
/// <param name="AmountOfResults">Maximum number of trucks to return (already checked against the allowed range).</param>
public sealed record FindFoodTrucksQuery(Coordinate Origin, int AmountOfResults);
