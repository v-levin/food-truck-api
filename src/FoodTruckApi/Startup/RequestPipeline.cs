using FoodTruckApi.Api.Middleware;
using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Configuration;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Startup;

/// <summary>Builds the middleware pipeline and warms up the singletons that must exist at startup.</summary>
internal static class RequestPipeline
{
    public static WebApplication UseFoodTruckApiPipeline(this WebApplication app)
    {
        app.WarmUp();
        app.LogEffectiveConfiguration();

        // Turn unhandled exceptions into RFC 9457 problem responses (no stack traces in prod).
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseSecurityHeaders();
        app.UseHttpLogging();
        app.UseConfiguredCors();
        app.UseRateLimiter();

        // Liveness/readiness probe; not rate-limited and not part of the API surface.
        app.MapHealthChecks("/health").DisableRateLimiting();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Force the dataset and the embedding index to load during startup rather than on the
    /// first request; a broken dataset then fails fast, and the embedding model is warm.
    /// </summary>
    private static void WarmUp(this WebApplication app)
    {
        app.Services.GetRequiredService<IFoodTruckRepository>();
        app.Services.GetRequiredService<FoodEmbeddingIndex>();
    }

    private static void LogEffectiveConfiguration(this WebApplication app)
    {
        var search = app.Services.GetRequiredService<IOptions<FoodTruckSearchOptions>>().Value;
        var matching = app.Services.GetRequiredService<IOptions<FoodMatchingOptions>>().Value;
        var rateLimit = app.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        app.Logger.LogInformation(
            "Configuration: results default {DefaultResults}, max {MaxResults}; match threshold {MatchThreshold} " +
            "(semantic {SemanticFloor}..{SemanticStrong}); rate limit {PermitLimit} requests / {WindowSeconds}s per client.",
            search.DefaultAmountOfResults,
            search.MaxAmountOfResults,
            matching.MatchThreshold,
            matching.SemanticFloor,
            matching.SemanticStrong,
            rateLimit.PermitLimit,
            rateLimit.WindowSeconds);
    }

    private static void UseConfiguredCors(this WebApplication app)
    {
        var allowedOrigins = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value.AllowedOrigins;
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        app.UseCors(policy => policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET")
            .AllowAnyHeader());
    }
}
