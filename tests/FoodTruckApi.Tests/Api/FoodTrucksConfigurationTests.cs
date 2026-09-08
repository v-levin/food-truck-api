using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Api;

public class FoodTrucksConfigurationTests
{
    [Fact]
    public async Task Default_amount_of_results_is_read_from_configuration()
    {
        await using var factory = new TestWebApplicationFactory().WithSettings(
            ("FoodTruckSearch:DefaultAmountOfResults", "3"));
        var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937");

        Assert.Equal(3, body.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Maximum_amount_of_results_is_read_from_configuration()
    {
        await using var factory = new TestWebApplicationFactory().WithSettings(
            ("FoodTruckSearch:DefaultAmountOfResults", "5"),
            ("FoodTruckSearch:MaxAmountOfResults", "5"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937&amountOfResults=6");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Startup_fails_when_the_default_exceeds_the_maximum()
    {
        using var factory = new TestWebApplicationFactory().WithSettings(
            ("FoodTruckSearch:DefaultAmountOfResults", "100"),
            ("FoodTruckSearch:MaxAmountOfResults", "50"));

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
