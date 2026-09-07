using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FoodTruckApi.Tests.Api;

public class HardeningTests
{
    private const string ValidQuery = "/api/food-trucks?latitude=37.7955&longitude=-122.3937";

    private static WebApplicationFactory<Program> FactoryWith(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(
                    settings.ToDictionary(s => s.Key, s => (string?)s.Value))));

    [Fact]
    public async Task Responses_carry_the_defensive_security_headers()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync(ValidQuery);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Exceeding_the_rate_limit_returns_429()
    {
        using var factory = FactoryWith(
            ("RateLimiting:PermitLimit", "3"),
            ("RateLimiting:WindowSeconds", "60"));
        var client = factory.CreateClient();

        HttpStatusCode? lastStatus = null;
        for (var i = 0; i < 5; i++)
        {
            lastStatus = (await client.GetAsync(ValidQuery)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task No_cors_headers_are_sent_by_default()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, ValidQuery);
        request.Headers.Add("Origin", "https://example.com");
        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task A_configured_origin_is_allowed()
    {
        using var factory = FactoryWith(("Cors:AllowedOrigins:0", "https://app.example.com"));
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, ValidQuery);
        request.Headers.Add("Origin", "https://app.example.com");
        var response = await client.SendAsync(request);

        Assert.Equal(
            "https://app.example.com",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
