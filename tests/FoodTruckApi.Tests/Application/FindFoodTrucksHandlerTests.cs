using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Geo;

namespace FoodTruckApi.Tests.Application;

public class FindFoodTrucksHandlerTests
{
    private static readonly HaversineDistanceCalculator Distance = new();

    // Origin near the SF Ferry Building; the trucks below sit at increasing distances.
    private static readonly double OriginLat = 37.7955;
    private static readonly double OriginLon = -122.3937;

    private static FindFoodTrucksHandler HandlerFor(params FoodTruck[] trucks) =>
        new(new InMemoryFoodTruckRepository(trucks), Distance);

    [Fact]
    public void Returns_trucks_ordered_by_distance_nearest_first()
    {
        var handler = HandlerFor(
            TestData.Truck("Far", 37.83, -122.28),
            TestData.Truck("Near", 37.796, -122.394),
            TestData.Truck("Middle", 37.78, -122.41));

        var result = handler.Handle(new FindFoodTrucksQuery(TestData.Point(OriginLat, OriginLon), AmountOfResults: 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Near", "Middle", "Far" }, result.Value.Select(n => n.Truck.Name));
        Assert.True(result.Value.SequenceEqual(result.Value.OrderBy(n => n.DistanceKm)));
    }

    [Fact]
    public void Never_returns_more_than_the_limit()
    {
        var handler = HandlerFor(
            TestData.Truck("A", 37.79, -122.39),
            TestData.Truck("B", 37.80, -122.40),
            TestData.Truck("C", 37.78, -122.41),
            TestData.Truck("D", 37.77, -122.42));

        var result = handler.Handle(new FindFoodTrucksQuery(TestData.Point(OriginLat, OriginLon), AmountOfResults: 2));

        Assert.Equal(2, result.Value.Count);
        Assert.Equal(new[] { "A", "B" }, result.Value.Select(n => n.Truck.Name));
    }

    [Fact]
    public void Returns_an_empty_success_when_there_are_no_trucks()
    {
        var handler = HandlerFor();

        var result = handler.Handle(new FindFoodTrucksQuery(TestData.Point(OriginLat, OriginLon), AmountOfResults: 10));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Populates_the_distance_for_each_result()
    {
        var handler = HandlerFor(TestData.Truck("Only", 37.7955, -122.3937));

        var result = handler.Handle(new FindFoodTrucksQuery(TestData.Point(OriginLat, OriginLon), AmountOfResults: 10));

        Assert.Equal(0d, Assert.Single(result.Value).DistanceKm, precision: 6);
    }
}
