using System.Reflection;
using FoodTruckApi.Api.Health;
using FoodTruckApi.Api.RateLimiting;
using FoodTruckApi.Api.Validation;
using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Configuration;
using FoodTruckApi.Infrastructure.Csv;
using FoodTruckApi.Infrastructure.Geo;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;

namespace FoodTruckApi.Startup;

/// <summary>Registers everything the API needs, grouped by concern.</summary>
internal static class ServiceRegistration
{
    public static IServiceCollection AddFoodTruckApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        // Every query parameter binds as a string, so model binding never rejects a
        // request on its own; keeping the auto-400 suppressed means that stays true even
        // if a typed property is added later, and every failure flows through the
        // validator into one Problem Details payload.
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        services.AddProblemDetails();

        services
            .AddValidatedOptions(configuration)
            .AddOpenApiDocs()
            .AddRequestLogging()
            .AddFoodTruckRateLimiter()
            .AddCors()
            .AddSearchServices();

        services.AddHealthChecks().AddCheck<DatasetHealthCheck>("dataset");

        return services;
    }

    private static IServiceCollection AddValidatedOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FoodTruckSearchOptions>()
            .Bind(configuration.GetSection(FoodTruckSearchOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<FoodMatchingOptions>()
            .Bind(configuration.GetSection(FoodMatchingOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddOpenApiDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlDocumentation = Path.Combine(
                AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlDocumentation))
            {
                options.IncludeXmlComments(xmlDocumentation);
            }
        });

        return services;
    }

    private static IServiceCollection AddRequestLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options => options.LoggingFields =
            HttpLoggingFields.RequestMethod
            | HttpLoggingFields.RequestPath
            | HttpLoggingFields.ResponseStatusCode
            | HttpLoggingFields.Duration);

        return services;
    }

    private static IServiceCollection AddSearchServices(this IServiceCollection services)
    {
        // The dataset is parsed once and served as an immutable snapshot for the app's lifetime.
        services.AddSingleton<IFoodTruckRepository>(serviceProvider =>
            CsvFoodTruckRepository.CreateFromEmbeddedDataset(
                serviceProvider.GetRequiredService<ILogger<CsvFoodTruckRepository>>()));

        services.AddSingleton<IDistanceCalculator, HaversineDistanceCalculator>();
        services.AddSingleton<IFoodEmbedder, LocalFoodEmbedder>();
        services.AddSingleton(serviceProvider => FoodEmbeddingIndex.Build(
            serviceProvider.GetRequiredService<IFoodTruckRepository>().GetAll(),
            serviceProvider.GetRequiredService<IFoodEmbedder>(),
            serviceProvider.GetRequiredService<ILogger<FoodEmbeddingIndex>>()));
        services.AddSingleton<IFoodMatcher, HybridFoodMatcher>();

        services.AddScoped<FindFoodTrucksHandler>();
        services.AddSingleton<FindFoodTrucksRequestValidator>();

        return services;
    }
}
