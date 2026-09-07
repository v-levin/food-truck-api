using FoodTruckApi.Infrastructure.Geo;

namespace FoodTruckApi.Tests.Infrastructure.Geo;

public class HaversineDistanceCalculatorTests
{
    private readonly HaversineDistanceCalculator _calculator = new();

    [Fact]
    public void Distance_between_a_point_and_itself_is_zero()
    {
        var point = TestData.Point(37.7749, -122.4194);

        Assert.Equal(0d, _calculator.DistanceKm(point, point));
    }

    [Fact]
    public void Distance_matches_a_known_reference_value()
    {
        var sanFrancisco = TestData.Point(37.7749, -122.4194);
        var oakland = TestData.Point(37.8044, -122.2712);

        var distance = _calculator.DistanceKm(sanFrancisco, oakland);

        // Reference: 13.4296 km computed independently.
        Assert.Equal(13.43, distance, precision: 2);
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        var a = TestData.Point(37.7749, -122.4194);
        var b = TestData.Point(37.7955, -122.3937);

        Assert.Equal(_calculator.DistanceKm(a, b), _calculator.DistanceKm(b, a), precision: 9);
    }
}
