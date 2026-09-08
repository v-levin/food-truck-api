using System.Diagnostics;
using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Domain;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// Holds a precomputed embedding for every truck's food listing, built once at startup.
/// Keyed by the dataset id so the matcher can look one up without re-embedding per request.
/// </summary>
internal sealed class FoodEmbeddingIndex
{
    private readonly IReadOnlyDictionary<string, float[]> _vectorsById;

    private FoodEmbeddingIndex(IReadOnlyDictionary<string, float[]> vectorsById) =>
        _vectorsById = vectorsById;

    public float[]? For(FoodTruck truck) => _vectorsById.GetValueOrDefault(truck.Id);

    public static FoodEmbeddingIndex Build(
        IReadOnlyList<FoodTruck> trucks,
        IFoodEmbedder embedder,
        ILogger<FoodEmbeddingIndex> logger)
    {
        var stopwatch = Stopwatch.StartNew();

        var vectorsById = new Dictionary<string, float[]>(trucks.Count, StringComparer.Ordinal);
        foreach (var truck in trucks)
        {
            vectorsById[truck.Id] = embedder.Embed(EmbeddingText(truck));
        }

        logger.LogInformation(
            "Embedded {Count} food listings in {ElapsedMs} ms.",
            vectorsById.Count,
            stopwatch.ElapsedMilliseconds);

        return new FoodEmbeddingIndex(vectorsById);
    }

    private static string EmbeddingText(FoodTruck truck) =>
        truck.Offering.ServesEverything ? "all kinds of food" : truck.FoodItems;
}
