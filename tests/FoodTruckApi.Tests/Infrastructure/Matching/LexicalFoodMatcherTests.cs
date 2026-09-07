using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Matching;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

public class LexicalFoodMatcherTests
{
    private readonly LexicalFoodMatcher _matcher = new();

    private static FoodTruck TruckServing(string foodItems) =>
        TestData.Truck("truck", 37.77, -122.42, foodItems);

    [Fact]
    public void Exact_term_scores_one()
    {
        Assert.Equal(1d, _matcher.Score("burritos", TruckServing("Tacos: Burritos: Quesadillas")));
    }

    [Fact]
    public void Singular_query_matches_plural_data()
    {
        Assert.Equal(1d, _matcher.Score("taco", TruckServing("Tacos: Burritos")));
    }

    [Fact]
    public void A_typo_still_matches_strongly()
    {
        var score = _matcher.Score("burito", TruckServing("Tacos: Burritos"));

        Assert.True(score >= 0.8d, $"expected a strong fuzzy match, got {score}");
    }

    [Fact]
    public void Unrelated_food_scores_low()
    {
        var score = _matcher.Score("sushi", TruckServing("Hot dogs: Burgers: Fries"));

        Assert.True(score < 0.5d, $"expected a weak match, got {score}");
    }

    [Fact]
    public void A_multi_word_query_matches_on_its_best_term()
    {
        var score = _matcher.Score("korean bbq", TruckServing("Korean food: rice plates"));

        Assert.True(score >= 0.9d, $"expected 'korean' to carry the match, got {score}");
    }

    [Fact]
    public void Blank_query_scores_zero()
    {
        Assert.Equal(0d, _matcher.Score("   ", TruckServing("Tacos")));
    }

    [Fact]
    public void Truck_without_food_terms_scores_zero()
    {
        Assert.Equal(0d, _matcher.Score("tacos", TruckServing(string.Empty)));
    }
}
