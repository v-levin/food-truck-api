using FoodTruckApi.Api.Validation;
using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Application.FindFoodTrucks;
using FoodTruckApi.Configuration;
using FoodTruckApi.Infrastructure.Csv;
using FoodTruckApi.Infrastructure.Geo;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOptions<FoodTruckSearchOptions>()
    .Bind(builder.Configuration.GetSection(FoodTruckSearchOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<FoodMatchingOptions>()
    .Bind(builder.Configuration.GetSection(FoodMatchingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory</c> can boot the app in integration tests.</summary>
public partial class Program;
