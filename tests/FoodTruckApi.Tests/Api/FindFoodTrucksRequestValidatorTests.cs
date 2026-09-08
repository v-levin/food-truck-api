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

    private static FindFoodTrucksRequest Request(
        string? latitude = "37.77",
        string? longitude = "-122.42",
        string? amountOfResults = null,
        string? food = null) =>
        new()
        {
            Latitude = latitude,
            Longitude = longitude,
            AmountOfResults = amountOfResults,
            Food = food,
        };

    [Fact]
    public void Uses_the_configured_default_when_amount_of_results_is_omitted()
    {
        var result = ValidatorWith(defaultAmount: 7).Validate(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.AmountOfResults);
    }

    [Fact]
    public void Enforces_the_configured_maximum()
    {
        var result = ValidatorWith(maxAmount: 20).Validate(Request(amountOfResults: "21"));

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("amountOfResults", error.Code);
        Assert.Contains("between 1 and 20", error.Message);
    }

    [Fact]
    public void Accepts_a_request_at_the_configured_maximum()
    {
        var result = ValidatorWith(maxAmount: 20).Validate(Request(amountOfResults: "20"));

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.AmountOfResults);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    public void Rejects_a_non_integer_amount_of_results(string amountOfResults)
    {
        var result = ValidatorWith().Validate(Request(amountOfResults: amountOfResults));

        // An empty value is "omitted" and falls back to the default; a garbage value is an error.
        if (amountOfResults.Length == 0)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.True(result.IsFailure);
            Assert.Contains(result.Errors, e => e.Code == "amountOfResults");
        }
    }

    [Fact]
    public void Collects_every_problem_in_one_pass()
    {
        var result = ValidatorWith().Validate(new FindFoodTrucksRequest
        {
            Latitude = "200",
            Longitude = null,
            AmountOfResults = "0",
            Food = new string('a', 101),
        });

        Assert.Equal(
            new[] { "amountOfResults", "food", "latitude", "longitude" },
            result.Errors.Select(e => e.Code).OrderBy(c => c, StringComparer.Ordinal));
    }

    [Fact]
    public void Trims_a_food_preference_and_passes_it_through()
    {
        var result = ValidatorWith().Validate(Request(food: "  Tacos  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Tacos", result.Value.Food);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Treats_a_blank_food_preference_as_no_preference(string? food)
    {
        var result = ValidatorWith().Validate(Request(food: food));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Food);
    }

    [Fact]
    public void Rejects_a_food_preference_over_the_length_cap()
    {
        var result = ValidatorWith().Validate(Request(food: new string('a', 101)));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "food");
    }

    [Theory]
    [InlineData("200", "0", "latitude")]
    [InlineData("0", "999", "longitude")]
    [InlineData("abc", "0", "latitude")]
    public void Rejects_an_unparseable_or_out_of_range_origin(string lat, string lon, string expectedCode)
    {
        var result = ValidatorWith().Validate(Request(latitude: lat, longitude: lon));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == expectedCode);
    }

    [Fact]
    public void A_bad_coordinate_and_a_bad_food_value_are_both_reported()
    {
        var result = ValidatorWith().Validate(Request(latitude: "200", food: new string('a', 101)));

        Assert.Contains(result.Errors, e => e.Code == "latitude");
        Assert.Contains(result.Errors, e => e.Code == "food");
    }
}
