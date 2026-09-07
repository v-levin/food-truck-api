using Microsoft.AspNetCore.Mvc;

namespace FoodTruckApi.Api.Contracts;

/// <summary>
/// Query-string parameters for <c>GET /api/food-trucks</c>. Everything is nullable so
/// model binding never fails on its own — validation is done explicitly and returns every
/// problem at once (see <c>FindFoodTrucksRequestValidator</c>).
/// </summary>
public sealed class FindFoodTrucksRequest
{
    /// <summary>Latitude of the search origin, in decimal degrees (-90 to 90). Required.</summary>
    [FromQuery(Name = "latitude")]
    public double? Latitude { get; init; }

    /// <summary>Longitude of the search origin, in decimal degrees (-180 to 180). Required.</summary>
    [FromQuery(Name = "longitude")]
    public double? Longitude { get; init; }

    /// <summary>
    /// How many trucks to return. Optional; when omitted the configured default is used
    /// (10 out of the box). Must be between 1 and the configured maximum (50 out of the box).
    /// </summary>
    [FromQuery(Name = "amountOfResults")]
    public int? AmountOfResults { get; init; }

    /// <summary>Food preference to filter by, e.g. <c>tacos</c>. Optional; omit to get every nearby truck.</summary>
    [FromQuery(Name = "food")]
    public string? Food { get; init; }
}
