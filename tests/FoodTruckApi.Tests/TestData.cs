using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;

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
            FoodItems: foodItems);
}

internal sealed class InMemoryFoodTruckRepository : IFoodTruckRepository
{
    private readonly IReadOnlyList<FoodTruck> _trucks;

    public InMemoryFoodTruckRepository(params FoodTruck[] trucks) => _trucks = trucks;

    public IReadOnlyList<FoodTruck> GetAll() => _trucks;
}
