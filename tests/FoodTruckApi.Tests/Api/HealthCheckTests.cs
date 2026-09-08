using System.Net;

namespace FoodTruckApi.Tests.Api;

public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_healthy_when_the_dataset_is_loaded()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_endpoint_is_not_rate_limited()
    {
        using var factory = new TestWebApplicationFactory().WithSettings(("RateLimiting:PermitLimit", "1"));
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }
    }
}
