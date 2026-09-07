namespace FoodTruckApi.Api.Contracts;

/// <summary>Response body for <c>GET /api/food-trucks</c>.</summary>
/// <param name="Query">Echo of the effective query (with defaults applied).</param>
/// <param name="Count">Number of items in <paramref name="Results"/>.</param>
/// <param name="Results">Matching trucks, nearest first.</param>
public sealed record FindFoodTrucksResponse(
    FoodTrucksQuery Query,
    int Count,
    IReadOnlyList<FoodTruckResult> Results);

/// <summary>The query the server actually ran, after defaults were applied.</summary>
public sealed record FoodTrucksQuery(double Latitude, double Longitude, int AmountOfResults);

/// <summary>A single food truck in the result set.</summary>
/// <param name="Name">Permit holder.</param>
/// <param name="FacilityType">Always <c>Truck</c>.</param>
/// <param name="Address">Street address of the pitch.</param>
/// <param name="Latitude">Pitch latitude.</param>
/// <param name="Longitude">Pitch longitude.</param>
/// <param name="DistanceKm">Great-circle distance from the requested origin, in kilometres.</param>
/// <param name="FoodItems">Raw food description from the permit.</param>
public sealed record FoodTruckResult(
    string Name,
    string FacilityType,
    string Address,
    double Latitude,
    double Longitude,
    double DistanceKm,
    string FoodItems);
