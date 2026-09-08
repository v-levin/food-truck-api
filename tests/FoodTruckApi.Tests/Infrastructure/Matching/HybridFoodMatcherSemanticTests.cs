using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

/// <summary>
/// Exercises the semantic tier with the real on-device model. Kept separate because it
/// loads the ONNX model (a one-off cost of ~1s for the class).
/// </summary>
[Trait("Category", "Semantic")]
public sealed class HybridFoodMatcherSemanticTests : IDisposable
{
    private readonly LocalFoodEmbedder _embedder = new();

    public void Dispose() => _embedder.Dispose();

    private Func<FoodTruck, double> Match(string query, params FoodTruck[] trucks)
    {
        var index = FoodEmbeddingIndex.Build(trucks, _embedder, NullLogger<FoodEmbeddingIndex>.Instance);
        var matcher = new HybridFoodMatcher(_embedder, index, Options.Create(new FoodMatchingOptions()));
        return matcher.ForQuery(query);
    }

    [Fact]
    public void Semantic_similarity_bridges_a_gap_that_words_and_aliases_miss()
    {
        // "italian" is not in the alias table and shares no words with either listing.
        var italian = TestData.Truck("Italian", 0, 0, "Wood-fired pizza: pasta: garlic knots");
        var hotDogs = TestData.Truck("Dogs", 0, 0, "Hot dogs: chili fries: soda");

        var score = Match("italian food", italian, hotDogs);

        Assert.True(score(italian) > 0d, "expected a non-zero semantic score for the pizza truck");
        Assert.True(score(italian) > score(hotDogs), "expected the pizza truck to outscore the hot dog truck");
    }
}
