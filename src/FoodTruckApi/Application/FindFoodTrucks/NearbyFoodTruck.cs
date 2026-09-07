using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.FindFoodTrucks;

/// <summary>A food truck paired with its distance from the requested origin.</summary>
public sealed record NearbyFoodTruck(FoodTruck Truck, double DistanceKm);
