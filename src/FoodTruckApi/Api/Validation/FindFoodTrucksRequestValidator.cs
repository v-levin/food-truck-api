using FoodTruckApi.Api.Contracts;
using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Domain.Common;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Api.Validation;

/// <summary>
/// Turns a raw <see cref="FindFoodTrucksRequest"/> into a validated
/// <see cref="FindFoodTrucksQuery"/>, collecting every problem rather than failing on the
/// first one. The default and maximum result counts come from configuration.
/// </summary>
public sealed class FindFoodTrucksRequestValidator
{
    internal const int MinAmountOfResults = 1;

    private readonly FoodTruckSearchOptions _options;

    public FindFoodTrucksRequestValidator(IOptions<FoodTruckSearchOptions> options) =>
        _options = options.Value;

    public Result<FindFoodTrucksQuery> Validate(FindFoodTrucksRequest request)
    {
        var errors = new List<Error>();

        if (request.Latitude is null)
        {
            errors.Add(Error.Validation(
                "latitude",
                "latitude is required and must be a number between -90 and 90."));
        }

        if (request.Longitude is null)
        {
            errors.Add(Error.Validation(
                "longitude",
                "longitude is required and must be a number between -180 and 180."));
        }

        var amountOfResults = request.AmountOfResults ?? _options.DefaultAmountOfResults;
        if (request.AmountOfResults is { } requested &&
            (requested < MinAmountOfResults || requested > _options.MaxAmountOfResults))
        {
            errors.Add(Error.Validation(
                "amountOfResults",
                $"amountOfResults must be between {MinAmountOfResults} and {_options.MaxAmountOfResults}."));
        }

        if (errors.Count > 0)
        {
            return Result.Failure<FindFoodTrucksQuery>(errors);
        }

        var origin = Coordinate.Create(request.Latitude!.Value, request.Longitude!.Value);
        if (origin.IsFailure)
        {
            // Re-key coordinate errors to the query parameter names the caller used.
            var reKeyed = origin.Errors
                .Select(e => e with { Code = e.Code.Replace("coordinate.", string.Empty) })
                .ToArray();
            return Result.Failure<FindFoodTrucksQuery>(reKeyed);
        }

        return new FindFoodTrucksQuery(origin.Value, amountOfResults);
    }
}
