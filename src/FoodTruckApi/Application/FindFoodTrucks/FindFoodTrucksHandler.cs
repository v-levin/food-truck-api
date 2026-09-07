using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Domain.Common;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<FindFoodTrucksHandler> _logger;

    public FindFoodTrucksHandler(
        IFoodTruckRepository repository,
        IDistanceCalculator distanceCalculator,
        IFoodMatcher foodMatcher,
        IOptions<FoodMatchingOptions> matchingOptions,
        ILogger<FindFoodTrucksHandler> logger)
    {
        _repository = repository;
        _distanceCalculator = distanceCalculator;
        _foodMatcher = foodMatcher;
        _matchingOptions = matchingOptions.Value;
        _logger = logger;
    }

    public Result<IReadOnlyList<NearbyFoodTruck>> Handle(FindFoodTrucksQuery query)
    {
        var all = _repository.GetAll();
        var matched = MatchFood(query.Food, all).ToArray();

        var nearest = matched
            .Select(match => new NearbyFoodTruck(
                match.Truck,
                _distanceCalculator.DistanceKm(query.Origin, match.Truck.Location),
                match.Score))
            .OrderBy(nearby => nearby.DistanceKm)
            .Take(query.AmountOfResults)
            .ToArray();

        // Coordinates are rounded to ~1 km so the logs are useful for debugging without
        // recording precise caller locations.
        _logger.LogInformation(
            "Food truck search near ~{Latitude},{Longitude} (food preference: {FoodPreference}) " +
            "matched {MatchedCount} of {TotalCount} trucks; returned {ReturnedCount}.",
            Math.Round(query.Origin.Latitude, 2),
            Math.Round(query.Origin.Longitude, 2),
            query.Food ?? "(none)",
            matched.Length,
            all.Count,
            nearest.Length);

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
