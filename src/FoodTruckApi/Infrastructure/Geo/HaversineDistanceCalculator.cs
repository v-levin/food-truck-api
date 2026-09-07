using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;

namespace FoodTruckApi.Infrastructure.Geo;

/// <summary>
/// Great-circle distance via the haversine formula on a spherical Earth. Accurate to a
/// few metres over city distances, which is far more than this ranking needs, and it is
/// stateless so it can be a singleton.
/// </summary>
internal sealed class HaversineDistanceCalculator : IDistanceCalculator
{
    // IUGG mean Earth radius.
    private const double EarthRadiusKm = 6371.0088;

    public double DistanceKm(Coordinate from, Coordinate to)
    {
        var deltaLatitude = ToRadians(to.Latitude - from.Latitude);
        var deltaLongitude = ToRadians(to.Longitude - from.Longitude);
        var fromLatitude = ToRadians(from.Latitude);
        var toLatitude = ToRadians(to.Latitude);

        var h = Square(Math.Sin(deltaLatitude / 2))
            + (Math.Cos(fromLatitude) * Math.Cos(toLatitude) * Square(Math.Sin(deltaLongitude / 2)));

        return EarthRadiusKm * 2 * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    private static double Square(double value) => value * value;

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180);
}
