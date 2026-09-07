using FoodTruckApi.Domain;

namespace FoodTruckApi.Tests.Domain;

public class CoordinateTests
{
    [Fact]
    public void Create_accepts_a_valid_point()
    {
        var result = Coordinate.Create(37.7749, -122.4194);

        Assert.True(result.IsSuccess);
        Assert.Equal(37.7749, result.Value.Latitude);
        Assert.Equal(-122.4194, result.Value.Longitude);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Create_accepts_boundary_values(double latitude, double longitude)
    {
        Assert.True(Coordinate.Create(latitude, longitude).IsSuccess);
    }

    [Theory]
    [InlineData(90.0001)]
    [InlineData(-90.0001)]
    [InlineData(double.NaN)]
    public void Create_rejects_an_out_of_range_latitude(double latitude)
    {
        var result = Coordinate.Create(latitude, 0);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "coordinate.latitude");
    }

    [Theory]
    [InlineData(180.0001)]
    [InlineData(-180.0001)]
    [InlineData(double.NaN)]
    public void Create_rejects_an_out_of_range_longitude(double longitude)
    {
        var result = Coordinate.Create(0, longitude);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "coordinate.longitude");
    }

    [Fact]
    public void Create_reports_both_axes_when_both_are_invalid()
    {
        var result = Coordinate.Create(-91, 200);

        Assert.Equal(2, result.Errors.Count);
    }
}
