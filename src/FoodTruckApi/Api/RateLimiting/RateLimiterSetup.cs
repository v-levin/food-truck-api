using System.Globalization;
using System.Threading.RateLimiting;
using FoodTruckApi.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Api.RateLimiting;

/// <summary>
/// Fixed-window rate limiting, per client, with the limits read from configuration on
/// every request so they can be changed without a restart.
/// </summary>
internal static class RateLimiterSetup
{
    public static IServiceCollection AddFoodTruckRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(PartitionFor);
            options.OnRejected = OnRejectedAsync;
        });

    private static RateLimitPartition<string> PartitionFor(HttpContext context)
    {
        var limits = CurrentLimits(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientKey(context, limits.ClientIdentifierHeader),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.PermitLimit,
                Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0,
            });
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var limits = CurrentLimits(context.HttpContext);

        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FoodTruckApi.RateLimiting")
            .LogWarning(
                "Rate limit exceeded for {ClientKey} on {RequestPath}.",
                ResolveClientKey(context.HttpContext, limits.ClientIdentifierHeader),
                context.HttpContext.Request.Path);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            // Round any positive remainder up to at least one second so a client that
            // honours the header does not immediately retry into the same window.
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter =
                seconds.ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests.",
                Detail = "Rate limit exceeded. Retry after the window resets.",
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }

    private static RateLimitingOptions CurrentLimits(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;

    /// <summary>
    /// The partition key: a trusted proxy header when one is configured, otherwise the
    /// direct connection IP.
    /// </summary>
    private static string ResolveClientKey(HttpContext context, string? clientIdentifierHeader)
    {
        if (!string.IsNullOrWhiteSpace(clientIdentifierHeader) &&
            context.Request.Headers.TryGetValue(clientIdentifierHeader, out var header))
        {
            var value = header.ToString();
            var comma = value.IndexOf(',');
            var first = (comma < 0 ? value : value[..comma]).Trim();
            if (first.Length > 0)
            {
                return first;
            }
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
