using System.Globalization;
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
    internal const int MaxFoodLength = 100;

    private readonly FoodTruckSearchOptions _options;

    public FindFoodTrucksRequestValidator(IOptions<FoodTruckSearchOptions> options) =>
        _options = options.Value;

    public Result<FindFoodTrucksQuery> Validate(FindFoodTrucksRequest request)
    {
        var errors = new List<Error>();

        var latitude = ParseCoordinate(request.Latitude, "latitude", -90d, 90d, errors);
        var longitude = ParseCoordinate(request.Longitude, "longitude", -180d, 180d, errors);
        var amountOfResults = ParseAmountOfResults(request.AmountOfResults, errors);
        var food = ParseFood(request.Food, errors);

        if (errors.Count > 0 || latitude is null || longitude is null)
        {
            return Result.Failure<FindFoodTrucksQuery>(errors);
        }

        // Ranges were already checked above, so this is defensive.
        var origin = Coordinate.Create(latitude.Value, longitude.Value);
        if (origin.IsSuccess)
        {
            return new FindFoodTrucksQuery(origin.Value, amountOfResults, food);
        }

        var reKeyed = origin.Errors
            .Select(e => e with { Code = e.Code.Replace("coordinate.", string.Empty) })
            .ToArray();
        return Result.Failure<FindFoodTrucksQuery>(reKeyed);
    }

    private static double? ParseCoordinate(
        string? raw,
        string field,
        double min,
        double max,
        List<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add(Error.Validation(field, $"{field} is required and must be a number between {min} and {max}."));
            return null;
        }

        if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || double.IsNaN(value)
            || double.IsInfinity(value))
        {
            errors.Add(Error.Validation(field, $"{field} must be a number between {min} and {max}."));
            return null;
        }

        if (value < min || value > max)
        {
            errors.Add(Error.Validation(field, $"{field} must be between {min} and {max}."));
            return null;
        }

        return value;
    }

    private int ParseAmountOfResults(string? raw, List<Error> errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return _options.DefaultAmountOfResults;
        }

        if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            errors.Add(Error.Validation(
                "amountOfResults",
                $"amountOfResults must be a whole number between {MinAmountOfResults} and {_options.MaxAmountOfResults}."));
            return _options.DefaultAmountOfResults;
        }

        if (value < MinAmountOfResults || value > _options.MaxAmountOfResults)
        {
            errors.Add(Error.Validation(
                "amountOfResults",
                $"amountOfResults must be between {MinAmountOfResults} and {_options.MaxAmountOfResults}."));
        }

        return value;
    }

    private static string? ParseFood(string? raw, List<Error> errors)
    {
        var food = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
        if (food is { Length: > MaxFoodLength })
        {
            errors.Add(Error.Validation("food", $"food must be {MaxFoodLength} characters or fewer."));
        }

        return food;
    }
}
