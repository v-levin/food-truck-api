using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Matching;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

public class LexicalFoodMatcherTests
{
    private readonly LexicalFoodMatcher _matcher = new();

    private double Score(string query, FoodTruck truck) => _matcher.ForQuery(query)(truck);

    private static FoodTruck TruckServing(string foodItems) =>
        TestData.Truck("truck", 37.77, -122.42, foodItems);

    [Fact]
    public void Exact_term_scores_one()
    {
        Assert.Equal(1d, Score("burritos", TruckServing("Tacos: Burritos: Quesadillas")));
    }

    [Fact]
    public void Singular_query_matches_plural_data()
    {
        Assert.Equal(1d, Score("taco", TruckServing("Tacos: Burritos")));
    }

    [Fact]
    public void A_typo_still_matches_strongly()
    {
        var score = Score("burito", TruckServing("Tacos: Burritos"));

        Assert.True(score >= 0.8d, $"expected a strong fuzzy match, got {score}");
    }

    [Fact]
    public void Unrelated_food_scores_low()
    {
        var score = Score("sushi", TruckServing("Hot dogs: Burgers: Fries"));

        Assert.True(score < 0.5d, $"expected a weak match, got {score}");
    }

    [Fact]
    public void A_multi_word_query_matches_on_its_best_term()
    {
        var score = Score("korean bbq", TruckServing("Korean food: rice plates"));

        Assert.True(score >= 0.9d, $"expected 'korean' to carry the match, got {score}");
    }

    [Theory]
    [InlineData("ice", "Rice Noodles: Fried Rice")]           // "ice" is a substring of "rice"
    [InlineData("ice cream", "Chinese Rice: Chow Mein")]      // still must not match on "ice"
    [InlineData("tea", "Steak sandwiches")]                   // 3 chars, near-substring
    public void A_short_query_term_does_not_match_a_longer_word_that_merely_contains_it(
        string query, string foodItems)
    {
        var score = Score(query, TruckServing(foodItems));

        Assert.True(score < 0.7d, $"expected no match for a short term, got {score}");
    }

    [Fact]
    public void Ice_cream_still_matches_an_actual_ice_cream_truck()
    {
        Assert.Equal(1d, Score("ice cream", TruckServing("Ice Cream: Waffle Cones")));
    }

    [Fact]
    public void A_root_word_matches_inside_a_compound()
    {
        var score = Score("shake", TruckServing("Milkshakes: Sundaes"));

        Assert.True(score >= 0.75d, $"expected 'shake' to match 'milkshake', got {score}");
    }

    [Fact]
    public void Blank_query_scores_zero()
    {
        Assert.Equal(0d, Score("   ", TruckServing("Tacos")));
    }

    [Fact]
    public void Truck_without_food_terms_scores_zero()
    {
        Assert.Equal(0d, Score("tacos", TruckServing(string.Empty)));
    }
}
