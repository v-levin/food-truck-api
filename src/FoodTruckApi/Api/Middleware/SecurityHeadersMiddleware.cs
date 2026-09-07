namespace FoodTruckApi.Api.Middleware;

/// <summary>
/// Adds a small set of defensive response headers. This is a JSON API with no browser UI,
/// so the headers are conservative: deny framing, don't sniff content types, send no
/// referrer, and disallow Flash/PDF cross-domain policies.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            return Task.CompletedTask;
        }, context);

        return _next(context);
    }
}

internal static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
