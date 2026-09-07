using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;

namespace FoodTruckApi.Infrastructure.Csv;

/// <summary>
/// Holds the parsed dataset as an immutable snapshot. Registered as a singleton and
/// populated once at startup, so reads are lock-free and thread-safe.
/// </summary>
internal sealed class CsvFoodTruckRepository : IFoodTruckRepository
{
    private readonly IReadOnlyList<FoodTruck> _trucks;

    public CsvFoodTruckRepository(IReadOnlyList<FoodTruck> trucks) => _trucks = trucks;

    /// <summary>Parses the embedded dataset and fails fast if it yields no usable trucks.</summary>
    public static CsvFoodTruckRepository CreateFromEmbeddedDataset(ILogger<CsvFoodTruckRepository> logger)
    {
        using var stream = EmbeddedFoodTruckDataset.Open();
        var (trucks, skippedRows) = FoodTruckCsvLoader.Load(stream);

        if (trucks.Count == 0)
        {
            throw new InvalidOperationException(
                "The food truck dataset produced no usable records after filtering. " +
                "The embedded CSV is missing or its schema has changed.");
        }

        logger.LogInformation(
            "Loaded {TruckCount} approved food trucks from the embedded dataset ({SkippedRows} rows skipped).",
            trucks.Count,
            skippedRows);

        return new CsvFoodTruckRepository(trucks);
    }

    public IReadOnlyList<FoodTruck> GetAll() => _trucks;
}
