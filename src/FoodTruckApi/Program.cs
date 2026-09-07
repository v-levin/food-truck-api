using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Infrastructure.Csv;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The dataset is parsed once and served as an immutable snapshot for the app's lifetime.
builder.Services.AddSingleton<IFoodTruckRepository>(serviceProvider =>
    CsvFoodTruckRepository.CreateFromEmbeddedDataset(
        serviceProvider.GetRequiredService<ILogger<CsvFoodTruckRepository>>()));

var app = builder.Build();

// Fail fast: load and validate the dataset during startup instead of on the first request.
app.Services.GetRequiredService<IFoodTruckRepository>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory</c> can boot the app in integration tests.</summary>
public partial class Program;
