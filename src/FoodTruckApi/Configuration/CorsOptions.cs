namespace FoodTruckApi.Configuration;

/// <summary>
/// Cross-origin access, bound from the <c>Cors</c> section. Empty (the default) means no
/// CORS headers are sent, so only same-origin browser callers are allowed.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Browser origins allowed to call the API, e.g. <c>https://app.example.com</c>.</summary>
    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
