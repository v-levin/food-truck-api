using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Api;

public class FoodTrucksConfigurationTests
{
    private static WebApplicationFactory<Program> FactoryWith(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(
                    settings.ToDictionary(s => s.Key, s => (string?)s.Value))));

    [Fact]
    public async Task Default_amount_of_results_is_read_from_configuration()
    {
        await using var factory = FactoryWith(("FoodTruckSearch:DefaultAmountOfResults", "3"));
        var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937");

        Assert.Equal(3, body.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Maximum_amount_of_results_is_read_from_configuration()
    {
        await using var factory = FactoryWith(
            ("FoodTruckSearch:DefaultAmountOfResults", "5"),
            ("FoodTruckSearch:MaxAmountOfResults", "5"));
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/food-trucks?latitude=37.7955&longitude=-122.3937&amountOfResults=6");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Startup_fails_when_the_default_exceeds_the_maximum()
    {
        await using var factory = FactoryWith(
            ("FoodTruckSearch:DefaultAmountOfResults", "100"),
            ("FoodTruckSearch:MaxAmountOfResults", "50"));

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
