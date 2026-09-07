using FoodTruckApi.Application.FindFoodTrucks;

namespace FoodTruckApi.Api.Contracts;

/// <summary>Maps the application result onto the API response shape.</summary>
internal static class FoodTruckResponseMapper
{
    private const int DistanceDecimals = 3;

    public static FindFoodTrucksResponse ToResponse(
        FindFoodTrucksQuery query,
        IReadOnlyList<NearbyFoodTruck> nearby)
    {
        var results = nearby.Select(ToResult).ToArray();

        return new FindFoodTrucksResponse(
            Query: new FoodTrucksQuery(
                query.Origin.Latitude,
                query.Origin.Longitude,
                query.AmountOfResults),
            Count: results.Length,
            Results: results);
    }

    private static FoodTruckResult ToResult(NearbyFoodTruck nearby)
    {
        var truck = nearby.Truck;

        return new FoodTruckResult(
            Name: truck.Name,
            FacilityType: truck.FacilityType,
            Address: truck.Address,
            Latitude: truck.Location.Latitude,
            Longitude: truck.Location.Longitude,
            DistanceKm: Math.Round(nearby.DistanceKm, DistanceDecimals),
            FoodItems: truck.FoodItems);
    }
}
