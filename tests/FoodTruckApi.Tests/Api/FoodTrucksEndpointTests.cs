using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FoodTruckApi.Tests.Api;

public class FoodTrucksEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public FoodTrucksEndpointTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Returns_the_requested_number_of_trucks_nearest_first()
    {
        var response = await _client.GetAsync(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937&amountOfResults=5");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<FindFoodTrucksResponseDto>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(5, body!.Count);
        Assert.Equal(5, body.Results.Count);
        Assert.Equal(5, body.Query.AmountOfResults);

        var distances = body.Results.Select(r => r.DistanceKm).ToArray();
        Assert.Equal(distances.OrderBy(d => d), distances);
        Assert.All(body.Results, r => Assert.Equal("Truck", r.FacilityType));
    }

    [Fact]
    public async Task Applies_the_default_amount_of_results_when_none_is_supplied()
    {
        var body = await _client.GetFromJsonAsync<FindFoodTrucksResponseDto>(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937", JsonOptions);

        Assert.Equal(10, body!.Count);
    }

    [Fact]
    public async Task Rejects_a_missing_latitude_with_a_validation_problem()
    {
        var response = await _client.GetAsync("/api/food-trucks?longitude=-122.3937");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDto>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Contains("latitude", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Reports_every_invalid_parameter_at_once()
    {
        var response = await _client.GetAsync("/api/food-trucks?amountOfResults=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDto>(JsonOptions);
        Assert.Equal(
            new[] { "amountOfResults", "latitude", "longitude" },
            problem!.Errors.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("latitude=91&longitude=0")]
    [InlineData("latitude=0&longitude=181")]
    [InlineData("latitude=37.79&longitude=-122.39&amountOfResults=51")]
    [InlineData("latitude=abc&longitude=-122.39")]
    public async Task Rejects_out_of_range_or_unparseable_values(string queryString)
    {
        var response = await _client.GetAsync($"/api/food-trucks?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record FindFoodTrucksResponseDto(
        FoodTrucksQueryDto Query,
        int Count,
        IReadOnlyList<FoodTruckResultDto> Results);

    private sealed record FoodTrucksQueryDto(double Latitude, double Longitude, int AmountOfResults);

    private sealed record FoodTruckResultDto(
        string Name,
        string FacilityType,
        string Address,
        double Latitude,
        double Longitude,
        double DistanceKm,
        string FoodItems);

    private sealed record ValidationProblemDto(Dictionary<string, string[]> Errors);
}
