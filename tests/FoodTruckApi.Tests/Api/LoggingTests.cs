using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace FoodTruckApi.Tests.Api;

public class LoggingTests
{
    private const string ValidQuery = "/api/food-trucks?latitude=37.7955&longitude=-122.3937";

    private static (TestWebApplicationFactory Factory, RecordingLoggerProvider Logs) Build(
        params (string Key, string Value)[] settings)
    {
        var logs = new RecordingLoggerProvider();
        var factory = new TestWebApplicationFactory().WithSettings(settings).CapturingLogsTo(logs);
        return (factory, logs);
    }

    [Fact]
    public async Task A_search_is_logged_with_its_match_counts()
    {
        var (factory, logs) = Build();
        using var _ = factory;

        await factory.CreateClient().GetFromJsonAsync<object>($"{ValidQuery}&food=tacos&amountOfResults=3");

        Assert.Contains(logs.Entries, e =>
            e.Category == "FoodTruckApi.Application.FindFoodTrucks.FindFoodTrucksHandler"
            && e.Level == LogLevel.Information
            && e.Message.Contains("food preference: tacos")
            && e.Message.Contains("of 158 trucks")
            && e.Message.Contains("returned 3"));
    }

    [Fact]
    public async Task A_rate_limit_rejection_is_logged_as_a_warning()
    {
        var (factory, logs) = Build(
            ("RateLimiting:PermitLimit", "2"),
            ("RateLimiting:WindowSeconds", "60"));
        using var _ = factory;
        var client = factory.CreateClient();

        for (var i = 0; i < 4; i++)
        {
            await client.GetAsync(ValidQuery);
        }

        Assert.Contains(logs.Entries, e =>
            e.Category == "FoodTruckApi.RateLimiting"
            && e.Level == LogLevel.Warning
            && e.Message.Contains("Rate limit exceeded"));
    }
}
