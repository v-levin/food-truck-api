using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Geo;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Application;

public class FindFoodTrucksHandlerTests
{
    private static readonly HaversineDistanceCalculator Distance = new();

    // Origin near the SF Ferry Building; the trucks below sit at increasing distances.
    private const double OriginLat = 37.7955;
    private const double OriginLon = -122.3937;

    private static FindFoodTrucksHandler HandlerFor(
        IFoodMatcher matcher,
        double threshold,
        params FoodTruck[] trucks) =>
        new(
            new InMemoryFoodTruckRepository(trucks),
            Distance,
            matcher,
            Options.Create(new FoodMatchingOptions { MatchThreshold = threshold }));

    private static FindFoodTrucksHandler HandlerFor(params FoodTruck[] trucks) =>
        HandlerFor(new StubFoodMatcher(new Dictionary<string, double>()), threshold: 0.6, trucks);

    private static FindFoodTrucksQuery Query(int amountOfResults = 10, string? food = null) =>
        new(TestData.Point(OriginLat, OriginLon), amountOfResults, food);

    [Fact]
    public void Returns_trucks_ordered_by_distance_nearest_first()
    {
        var handler = HandlerFor(
            TestData.Truck("Far", 37.83, -122.28),
            TestData.Truck("Near", 37.796, -122.394),
            TestData.Truck("Middle", 37.78, -122.41));

        var result = handler.Handle(Query());

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Near", "Middle", "Far" }, result.Value.Select(n => n.Truck.Name));
    }

    [Fact]
    public void Never_returns_more_than_the_requested_amount()
    {
        var handler = HandlerFor(
            TestData.Truck("A", 37.79, -122.39),
            TestData.Truck("B", 37.80, -122.40),
            TestData.Truck("C", 37.78, -122.41),
            TestData.Truck("D", 37.77, -122.42));

        var result = handler.Handle(Query(amountOfResults: 2));

        Assert.Equal(new[] { "A", "B" }, result.Value.Select(n => n.Truck.Name));
    }

    [Fact]
    public void Without_a_food_preference_every_truck_is_a_candidate_and_score_is_null()
    {
        var handler = HandlerFor(
            TestData.Truck("A", 37.79, -122.39),
            TestData.Truck("B", 37.80, -122.40));

        var result = handler.Handle(Query(food: null));

        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, n => Assert.Null(n.MatchScore));
    }

    [Fact]
    public void With_a_food_preference_only_trucks_at_or_above_the_threshold_are_returned()
    {
        var matcher = new StubFoodMatcher(new Dictionary<string, double>
        {
            ["Match"] = 0.9,
            ["Borderline"] = 0.6,
            ["Miss"] = 0.59,
        });
        var handler = HandlerFor(
            matcher,
            threshold: 0.6,
            TestData.Truck("Match", 37.80, -122.40),
            TestData.Truck("Borderline", 37.79, -122.39),
            TestData.Truck("Miss", 37.796, -122.394));

        var result = handler.Handle(Query(food: "tacos"));

        Assert.Equal(new[] { "Borderline", "Match" }, result.Value.Select(n => n.Truck.Name));
    }

    [Fact]
    public void Food_matched_results_carry_their_score_and_stay_ordered_by_distance()
    {
        var matcher = new StubFoodMatcher(new Dictionary<string, double>
        {
            ["Close"] = 0.7,
            ["FarButBetterMatch"] = 1.0,
        });
        var handler = HandlerFor(
            matcher,
            threshold: 0.6,
            TestData.Truck("FarButBetterMatch", 37.83, -122.28),
            TestData.Truck("Close", 37.796, -122.394));

        var result = handler.Handle(Query(food: "tacos"));

        Assert.Equal(new[] { "Close", "FarButBetterMatch" }, result.Value.Select(n => n.Truck.Name));
        Assert.Equal(0.7, result.Value[0].MatchScore);
        Assert.Equal(1.0, result.Value[1].MatchScore);
    }

    [Fact]
    public void Returns_an_empty_success_when_no_truck_matches_the_food()
    {
        var matcher = new StubFoodMatcher(new Dictionary<string, double>(), @default: 0d);
        var handler = HandlerFor(
            matcher,
            threshold: 0.6,
            TestData.Truck("A", 37.79, -122.39));

        var result = handler.Handle(Query(food: "sushi"));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
