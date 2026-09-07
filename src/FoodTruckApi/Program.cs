using System.Threading.RateLimiting;
using FoodTruckApi.Api.Middleware;
using FoodTruckApi.Api.Validation;
using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Configuration;
using FoodTruckApi.Infrastructure.Csv;
using FoodTruckApi.Infrastructure.Geo;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Don't advertise the server software.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddControllers();

builder.Services.AddOptions<FoodTruckSearchOptions>()
    .Bind(builder.Configuration.GetSection(FoodTruckSearchOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<FoodMatchingOptions>()
    .Bind(builder.Configuration.GetSection(FoodMatchingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName));

// Own all input validation ourselves so failures flow through the Result pipeline and
// come back as a single Problem Details payload listing every problem.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlDocumentation = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlDocumentation))
    {
        options.IncludeXmlComments(xmlDocumentation);
    }
});

builder.Services.AddHttpLogging(options => options.LoggingFields =
    HttpLoggingFields.RequestMethod
    | HttpLoggingFields.RequestPath
    | HttpLoggingFields.ResponseStatusCode
    | HttpLoggingFields.Duration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var limits = context.RequestServices
            .GetRequiredService<IOptionsMonitor<RateLimitingOptions>>()
            .CurrentValue;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientKey(context, limits.ClientIdentifierHeader),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limits.PermitLimit,
                Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                QueueLimit = 0,
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FoodTruckApi.RateLimiting")
            .LogWarning(
                "Rate limit exceeded for {ClientIp} on {RequestPath}.",
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.HttpContext.Request.Path);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests.",
                Detail = "Rate limit exceeded. Retry after the window resets.",
            },
            cancellationToken);
    };
});

builder.Services.AddCors();

// The dataset is parsed once and served as an immutable snapshot for the app's lifetime.
builder.Services.AddSingleton<IFoodTruckRepository>(serviceProvider =>
    CsvFoodTruckRepository.CreateFromEmbeddedDataset(
        serviceProvider.GetRequiredService<ILogger<CsvFoodTruckRepository>>()));

builder.Services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
builder.Services.AddSingleton<IFoodMatcher, LexicalFoodMatcher>();
builder.Services.AddScoped<FindFoodTrucksHandler>();
builder.Services.AddSingleton<FindFoodTrucksRequestValidator>();

var app = builder.Build();

// Fail fast: load and validate the dataset during startup instead of on the first request.
app.Services.GetRequiredService<IFoodTruckRepository>();

var searchOptions = app.Services.GetRequiredService<IOptions<FoodTruckSearchOptions>>().Value;
var matchingOptions = app.Services.GetRequiredService<IOptions<FoodMatchingOptions>>().Value;
var rateLimitOptions = app.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
app.Logger.LogInformation(
    "Configuration: results default {DefaultResults}, max {MaxResults}; match threshold {MatchThreshold}; " +
    "rate limit {PermitLimit} requests / {WindowSeconds}s per client.",
    searchOptions.DefaultAmountOfResults,
    searchOptions.MaxAmountOfResults,
    matchingOptions.MatchThreshold,
    rateLimitOptions.PermitLimit,
    rateLimitOptions.WindowSeconds);

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

var corsOrigins = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value.AllowedOrigins;
if (corsOrigins.Length > 0)
{
    app.UseCors(policy => policy
        .WithOrigins(corsOrigins)
        .WithMethods("GET")
        .AllowAnyHeader());
}

app.UseRateLimiter();

app.MapControllers();

app.Run();

// Picks the rate-limit partition key: a trusted proxy header if one is configured,
// otherwise the direct connection IP.
static string ResolveClientKey(HttpContext context, string? clientIdentifierHeader)
{
    if (!string.IsNullOrWhiteSpace(clientIdentifierHeader) &&
        context.Request.Headers.TryGetValue(clientIdentifierHeader, out var header))
    {
        var first = header.ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (!string.IsNullOrEmpty(first))
        {
            return first;
        }
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

/// <summary>Exposed so <c>WebApplicationFactory</c> can boot the app in integration tests.</summary>
public partial class Program;
