using System.ComponentModel.DataAnnotations;

namespace FoodTruckApi.Configuration;

/// <summary>
/// Fixed-window rate limiting, per client IP, bound from the <c>RateLimiting</c> section
/// and validated at startup.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per window, per client.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; } = 100;

    /// <summary>Length of the window in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; init; } = 60;
}
