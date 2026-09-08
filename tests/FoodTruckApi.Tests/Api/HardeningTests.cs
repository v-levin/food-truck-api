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

        HttpResponseMessage? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await client.GetAsync(ValidQuery);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("application/problem+json", last.Content.Headers.ContentType?.MediaType);

        // Retry-After must be a whole number of at least 1 second, never 0.
        var retryAfter = Assert.Single(last.Headers.GetValues("Retry-After"));
        Assert.True(int.TryParse(retryAfter, out var seconds) && seconds >= 1, $"Retry-After was '{retryAfter}'");
    }

    [Fact]
    public async Task Rate_limit_partitions_by_the_configured_client_header()
    {
        using var factory = FactoryWith(
            ("RateLimiting:PermitLimit", "2"),
            ("RateLimiting:WindowSeconds", "60"),
            ("RateLimiting:ClientIdentifierHeader", "X-Forwarded-For"));
        var client = factory.CreateClient();

        // "clientA" uses its whole allowance...
        for (var i = 0; i < 3; i++)
        {
            var exhaust = new HttpRequestMessage(HttpMethod.Get, ValidQuery);
            exhaust.Headers.Add("X-Forwarded-For", "clientA");
            await client.SendAsync(exhaust);
        }

        // ...and "clientB" is still unaffected.
        var other = new HttpRequestMessage(HttpMethod.Get, ValidQuery);
        other.Headers.Add("X-Forwarded-For", "clientB");
        var response = await client.SendAsync(other);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
