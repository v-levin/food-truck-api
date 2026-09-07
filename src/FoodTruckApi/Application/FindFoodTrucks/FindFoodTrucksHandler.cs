using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Domain.Common;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Application.FindFoodTrucks;

/// <summary>
/// Finds the trucks nearest to the requested origin, optionally filtered by a food
/// preference. At ~160 records a linear scan plus sort is well under a millisecond, so
/// there is no index or spatial structure.
/// </summary>
public sealed class FindFoodTrucksHandler
{
    private readonly IFoodTruckRepository _repository;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly IFoodMatcher _foodMatcher;
    private readonly FoodMatchingOptions _matchingOptions;

    public FindFoodTrucksHandler(
        IFoodTruckRepository repository,
        IDistanceCalculator distanceCalculator,
        IFoodMatcher foodMatcher,
        IOptions<FoodMatchingOptions> matchingOptions)
    {
        _repository = repository;
        _distanceCalculator = distanceCalculator;
        _foodMatcher = foodMatcher;
        _matchingOptions = matchingOptions.Value;
    }

    public Result<IReadOnlyList<NearbyFoodTruck>> Handle(FindFoodTrucksQuery query)
    {
        var matched = MatchFood(query.Food, _repository.GetAll());

        var nearest = matched
            .Select(match => new NearbyFoodTruck(
                match.Truck,
                _distanceCalculator.DistanceKm(query.Origin, match.Truck.Location),
                match.Score))
            .OrderBy(nearby => nearby.DistanceKm)
            .Take(query.AmountOfResults)
            .ToArray();

        return Result.Success<IReadOnlyList<NearbyFoodTruck>>(nearest);
    }

    private IEnumerable<(FoodTruck Truck, double? Score)> MatchFood(
        string? food,
        IReadOnlyList<FoodTruck> trucks)
    {
        if (string.IsNullOrWhiteSpace(food))
        {
            return trucks.Select(truck => (truck, (double?)null));
        }

        return trucks
            .Select(truck => (Truck: truck, Score: _foodMatcher.Score(food, truck)))
            .Where(match => match.Score >= _matchingOptions.MatchThreshold)
            .Select(match => (match.Truck, (double?)match.Score));
    }
}
