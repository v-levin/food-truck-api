using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Matching;

namespace FoodTruckApi.Tests;

internal static class TestData
{
    public static Coordinate Point(double latitude, double longitude) =>
        Coordinate.Create(latitude, longitude).Value;

    public static FoodTruck Truck(
        string name,
        double latitude,
        double longitude,
        string foodItems = "Tacos: Burritos") =>
        new(
            Id: name,
            Name: name,
            FacilityType: "Truck",
            Address: $"{name} Street",
            Location: Point(latitude, longitude),
            FoodItems: foodItems)
        {
            FoodTerms = FoodTextNormalizer.ExtractTerms(foodItems),
        };
}

/// <summary>An <see cref="IFoodMatcher"/> that returns scripted scores keyed by truck name.</summary>
internal sealed class StubFoodMatcher : IFoodMatcher
{
    private readonly IReadOnlyDictionary<string, double> _scoresByTruckName;
    private readonly double _default;

    public StubFoodMatcher(IReadOnlyDictionary<string, double> scoresByTruckName, double @default = 0d)
    {
        _scoresByTruckName = scoresByTruckName;
        _default = @default;
    }

    public double Score(string foodQuery, FoodTruck truck) =>
        _scoresByTruckName.TryGetValue(truck.Name, out var score) ? score : _default;
}

internal sealed class InMemoryFoodTruckRepository : IFoodTruckRepository
{
    private readonly IReadOnlyList<FoodTruck> _trucks;

    public InMemoryFoodTruckRepository(params FoodTruck[] trucks) => _trucks = trucks;

    public IReadOnlyList<FoodTruck> GetAll() => _trucks;
}
