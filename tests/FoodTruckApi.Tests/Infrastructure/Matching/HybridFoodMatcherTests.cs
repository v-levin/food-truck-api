using FoodTruckApi.Application.Abstractions;
using FoodTruckApi.Configuration;
using FoodTruckApi.Domain;
using FoodTruckApi.Infrastructure.Matching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

public class HybridFoodMatcherTests
{
    // A stub embedder makes semantic scores negligible, so these tests isolate the
    // catch-all / lexical / alias / fuzzy tiers.
    private static readonly IFoodEmbedder Embedder = new StubFoodEmbedder();

    private static Func<FoodTruck, double> Match(string query, params FoodTruck[] trucks)
    {
        var index = FoodEmbeddingIndex.Build(trucks, Embedder, NullLogger<FoodEmbeddingIndex>.Instance);
        var matcher = new HybridFoodMatcher(Embedder, index, Options.Create(new FoodMatchingOptions()));
        return matcher.ForQuery(query);
    }

    private static FoodTruck Serving(string foodItems) => TestData.Truck("t", 0, 0, foodItems);

    [Fact]
    public void Exact_term_scores_one()
    {
        var truck = Serving("Tacos: Burritos");
        Assert.Equal(1d, Match("burritos", truck)(truck));
    }

    [Fact]
    public void An_alias_matches_a_synonym()
    {
        var truck = Serving("Barbecue brisket: pulled pork: ribs");
        Assert.True(Match("bbq", truck)(truck) >= 0.9d);
    }

    [Fact]
    public void An_alias_matches_a_cuisine_to_its_dishes()
    {
        var truck = Serving("Tacos: Burritos: Quesadillas");
        Assert.True(Match("mexican", truck)(truck) >= 0.9d);
    }

    [Fact]
    public void A_catch_all_truck_matches_an_unlisted_food()
    {
        var truck = Serving("everything except for hot dogs");
        Assert.True(Match("tacos", truck)(truck) >= 0.8d);
    }

    [Theory]
    [InlineData("hot dogs")]
    [InlineData("frankfurters")]   // a synonym of the excluded food
    [InlineData("bratwurst")]
    public void A_catch_all_truck_still_excludes_what_it_rules_out(string query)
    {
        var truck = Serving("everything except for hot dogs");
        Assert.Equal(0d, Match(query, truck)(truck));
    }

    [Fact]
    public void A_catch_all_truck_that_also_lists_a_food_scores_that_food_fully()
    {
        var truck = Serving("Everything: Tacos");
        Assert.Equal(1d, Match("tacos", truck)(truck));
    }

    [Fact]
    public void A_typo_still_matches_strongly()
    {
        var truck = Serving("Tacos: Burritos");
        Assert.True(Match("burito", truck)(truck) >= 0.8d);
    }

    [Fact]
    public void A_short_query_term_does_not_match_a_word_that_merely_contains_it()
    {
        var truck = Serving("Rice Noodles: Fried Rice");
        Assert.True(Match("ice", truck)(truck) < 0.7d);
    }

    [Fact]
    public void Unrelated_food_scores_low()
    {
        var truck = Serving("Hot dogs: Burgers: Fries");
        Assert.True(Match("sushi", truck)(truck) < 0.5d);
    }

    [Fact]
    public void A_blank_query_scores_zero()
    {
        var truck = Serving("Tacos");
        Assert.Equal(0d, Match("   ", truck)(truck));
    }
}
