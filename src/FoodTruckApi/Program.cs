using FoodTruckApi.Startup;

var builder = WebApplication.CreateBuilder(args);

// Don't advertise the server software.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddFoodTruckApi(builder.Configuration);

var app = builder.Build();

app.UseFoodTruckApiPipeline();

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory</c> can boot the app in integration tests.</summary>
public partial class Program;
