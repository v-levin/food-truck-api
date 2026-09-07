using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain.Common;

namespace FoodTruckApi.Application.FindFoodTrucks;

/// <summary>
/// Finds the trucks nearest to the requested origin. At ~160 records a linear scan plus
/// sort is well under a millisecond, so there is no index or spatial structure.
/// </summary>
public sealed class FindFoodTrucksHandler
{
    private readonly IFoodTruckRepository _repository;
    private readonly IDistanceCalculator _distanceCalculator;

    public FindFoodTrucksHandler(IFoodTruckRepository repository, IDistanceCalculator distanceCalculator)
    {
        _repository = repository;
        _distanceCalculator = distanceCalculator;
    }

    public Result<IReadOnlyList<NearbyFoodTruck>> Handle(FindFoodTrucksQuery query)
    {
        var nearest = _repository.GetAll()
            .Select(truck => new NearbyFoodTruck(
                truck,
                _distanceCalculator.DistanceKm(query.Origin, truck.Location)))
            .OrderBy(nearby => nearby.DistanceKm)
            .Take(query.AmountOfResults)
            .ToArray();

        return Result.Success<IReadOnlyList<NearbyFoodTruck>>(nearest);
    }
}
