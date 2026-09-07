using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.Abstractions;

/// <summary>Computes the distance between two points on Earth.</summary>
public interface IDistanceCalculator
{
    /// <summary>Great-circle distance in kilometres.</summary>
    double DistanceKm(Coordinate from, Coordinate to);
}
