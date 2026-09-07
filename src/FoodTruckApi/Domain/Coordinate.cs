using FoodTruckApi.Domain.Common;

namespace FoodTruckApi.Domain;

/// <summary>
/// A validated WGS-84 point. Construct through <see cref="Create"/>; the constructor is
/// private so an invalid coordinate cannot exist.
/// </summary>
public sealed record Coordinate
{
    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinate> Create(double latitude, double longitude)
    {
        var errors = new List<Error>(2);

        if (double.IsNaN(latitude) || latitude is < -90d or > 90d)
        {
            errors.Add(Error.Validation("coordinate.latitude", "Latitude must be between -90 and 90."));
        }

        if (double.IsNaN(longitude) || longitude is < -180d or > 180d)
        {
            errors.Add(Error.Validation("coordinate.longitude", "Longitude must be between -180 and 180."));
        }

        return errors.Count > 0
            ? Result.Failure<Coordinate>(errors)
            : new Coordinate(latitude, longitude);
    }
}
