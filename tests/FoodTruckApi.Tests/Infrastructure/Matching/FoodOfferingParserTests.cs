using FoodTruckApi.Infrastructure.Matching;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

public class FoodOfferingParserTests
{
    [Fact]
    public void A_normal_listing_becomes_positive_terms()
    {
        var offering = FoodOfferingParser.Parse("Tacos: Burritos: Quesadillas");

        Assert.False(offering.ServesEverything);
        Assert.Empty(offering.Excludes);
        Assert.Equal(new[] { "taco", "burrito", "quesadilla" }, offering.Terms);
    }

    [Theory]
    [InlineData("Everything")]
    [InlineData("Multiple Food Trucks & Food Types")]
    public void A_catch_all_listing_sets_the_flag(string foodItems)
    {
        var offering = FoodOfferingParser.Parse(foodItems);

        Assert.True(offering.ServesEverything);
        Assert.Empty(offering.Excludes);
    }

    [Theory]
    [InlineData("everything except for hot dogs")]
    [InlineData("everything but hot dogs")]
    public void An_exclusion_is_parsed_into_excluded_terms(string foodItems)
    {
        var offering = FoodOfferingParser.Parse(foodItems);

        Assert.True(offering.ServesEverything);
        Assert.Contains("dog", offering.Excludes);
        Assert.DoesNotContain("dog", offering.Terms);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Blank_input_is_the_empty_offering(string? foodItems)
    {
        var offering = FoodOfferingParser.Parse(foodItems);

        Assert.False(offering.ServesEverything);
        Assert.Empty(offering.Terms);
        Assert.Empty(offering.Excludes);
    }
}
