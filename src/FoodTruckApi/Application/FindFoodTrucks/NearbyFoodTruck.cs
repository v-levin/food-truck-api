using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.FindFoodTrucks;

/// <summary>A food truck paired with its distance from the requested origin.</summary>
/// <param name="Truck">The matched truck.</param>
/// <param name="DistanceKm">Great-circle distance from the requested origin.</param>
/// <param name="MatchScore">
/// Food match score (0..1) when a food preference was supplied; null otherwise.
/// </param>
public sealed record NearbyFoodTruck(FoodTruck Truck, double DistanceKm, double? MatchScore = null);
