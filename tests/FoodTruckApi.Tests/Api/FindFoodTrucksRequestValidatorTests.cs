using FoodTruckApi.Api.Contracts;
using FoodTruckApi.Api.Validation;
using FoodTruckApi.Configuration;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Api;

public class FindFoodTrucksRequestValidatorTests
{
    private static FindFoodTrucksRequestValidator ValidatorWith(
        int defaultAmount = 10,
        int maxAmount = 50) =>
        new(Options.Create(new FoodTruckSearchOptions
        {
            DefaultAmountOfResults = defaultAmount,
            MaxAmountOfResults = maxAmount,
        }));

    [Fact]
    public void Uses_the_configured_default_when_amount_of_results_is_omitted()
    {
        var result = ValidatorWith(defaultAmount: 7).Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.AmountOfResults);
    }

    [Fact]
    public void Enforces_the_configured_maximum()
    {
        var result = ValidatorWith(maxAmount: 20).Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
            AmountOfResults = 21,
        });

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("amountOfResults", error.Code);
        Assert.Contains("between 1 and 20", error.Message);
    }

    [Fact]
    public void Accepts_a_request_at_the_configured_maximum()
    {
        var result = ValidatorWith(maxAmount: 20).Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
            AmountOfResults = 20,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.AmountOfResults);
    }

    [Fact]
    public void Collects_every_problem_in_one_pass()
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest { AmountOfResults = 0 });

        Assert.Equal(
            new[] { "amountOfResults", "latitude", "longitude" },
            result.Errors.Select(e => e.Code).OrderBy(c => c, StringComparer.Ordinal));
    }

    [Fact]
    public void Trims_a_food_preference_and_passes_it_through()
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
            Food = "  Tacos  ",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Tacos", result.Value.Food);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Treats_a_blank_food_preference_as_no_preference(string? food)
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
            Food = food,
        });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Food);
    }

    [Fact]
    public void Rejects_a_food_preference_over_the_length_cap()
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest
        {
            Latitude = 37.77,
            Longitude = -122.42,
            Food = new string('a', 101),
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "food");
    }

    [Fact]
    public void Rejects_an_origin_outside_valid_coordinate_ranges()
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest
        {
            Latitude = 200,
            Longitude = 0,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "latitude");
    }
}
