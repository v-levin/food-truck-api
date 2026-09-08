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
            Offering = FoodOfferingParser.Parse(foodItems),
        };
}

/// <summary>
/// A fast, deterministic <see cref="IFoodEmbedder"/> for tests that boot the app but don't
/// exercise semantic matching — avoids loading the real ONNX model per test class.
/// </summary>
internal sealed class StubFoodEmbedder : IFoodEmbedder
{
    public float[] Embed(string text)
    {
        var vector = new float[384];
        uint hash = 2166136261;
        foreach (var ch in text ?? string.Empty)
        {
            hash = (hash ^ ch) * 16777619;
        }

        var random = new Random((int)hash);
        double sumOfSquares = 0d;
        for (var i = 0; i < vector.Length; i++)
        {
            var component = (float)(random.NextDouble() * 2d - 1d);
            vector[i] = component;
            sumOfSquares += component * (double)component;
        }

        var magnitude = Math.Sqrt(sumOfSquares);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / magnitude);
        }

        return vector;
    }
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

    public Func<FoodTruck, double> ForQuery(string foodQuery) =>
        truck => _scoresByTruckName.TryGetValue(truck.Name, out var score) ? score : _default;
}

internal sealed class InMemoryFoodTruckRepository : IFoodTruckRepository
{
    private readonly IReadOnlyList<FoodTruck> _trucks;

    public InMemoryFoodTruckRepository(params FoodTruck[] trucks) => _trucks = trucks;

    public IReadOnlyList<FoodTruck> GetAll() => _trucks;
}
