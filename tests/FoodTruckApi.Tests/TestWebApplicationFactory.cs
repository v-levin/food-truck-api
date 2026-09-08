using FoodTruckApi.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FoodTruckApi.Tests;

/// <summary>
/// Boots the app for integration tests with the real ONNX embedder swapped for a fast
/// stub. Configure with the fluent methods before calling <c>CreateClient</c>.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private Dictionary<string, string?> _settings = new();
    private ILoggerProvider? _loggerProvider;

    /// <summary>Override configuration values (e.g. <c>("RateLimiting:PermitLimit", "1")</c>).</summary>
    public TestWebApplicationFactory WithSettings(params (string Key, string Value)[] settings)
    {
        _settings = settings.ToDictionary(s => s.Key, s => (string?)s.Value);
        return this;
    }

    /// <summary>Attach an extra log provider, for tests that assert on log output.</summary>
    public TestWebApplicationFactory CapturingLogsTo(ILoggerProvider provider)
    {
        _loggerProvider = provider;
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_settings));

        if (_loggerProvider is not null)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(_loggerProvider));
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFoodEmbedder>();
            services.AddSingleton<IFoodEmbedder, StubFoodEmbedder>();
        });
    }
}
