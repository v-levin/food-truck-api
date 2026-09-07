using FoodTruckApi.Domain;

namespace FoodTruckApi.Application.Abstractions;

/// <summary>
/// Read-only access to the filtered food truck dataset. Implementations are expected to
/// load once and serve an immutable snapshot.
/// </summary>
public interface IFoodTruckRepository
{
    /// <summary>Every approved truck that has a valid location.</summary>
    IReadOnlyList<FoodTruck> GetAll();
}
