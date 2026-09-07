using FoodTruckApi.Infrastructure.Matching;

namespace FoodTruckApi.Tests.Infrastructure.Matching;

public class FoodTextNormalizerTests
{
    [Fact]
    public void Splits_a_colon_delimited_description_into_stemmed_terms()
    {
        var terms = FoodTextNormalizer.ExtractTerms("Tacos: Burritos: Quesadillas: Tortas");

        Assert.Equal(new[] { "taco", "burrito", "quesadilla", "torta" }, terms);
    }

    [Theory]
    [InlineData("Noodles & Meat", new[] { "noodl", "meat" })]
    [InlineData("Coffee, Tea; Pastries", new[] { "coffe", "tea", "pastri" })]
    [InlineData("Hot Dogs and Sausages", new[] { "dog", "sausag" })] // "hot" and "and" are noise words
    public void Handles_the_various_separators_and_drops_noise_words(string input, string[] expected)
    {
        Assert.Equal(expected, FoodTextNormalizer.ExtractTerms(input));
    }

    [Fact]
    public void Lower_cases_and_strips_surrounding_punctuation()
    {
        Assert.Equal(new[] { "taco", "burger" }, FoodTextNormalizer.ExtractTerms("TACOS! (BURGERS)"));
    }

    [Fact]
    public void Deduplicates_repeated_terms()
    {
        Assert.Equal(new[] { "taco" }, FoodTextNormalizer.ExtractTerms("Tacos: tacos : TACO"));
    }

    [Theory]
    [InlineData("", new string[0])]
    [InlineData("   ", new string[0])]
    [InlineData(null, new string[0])]
    public void Returns_nothing_for_blank_input(string? input, string[] expected)
    {
        Assert.Equal(expected, FoodTextNormalizer.ExtractTerms(input));
    }

    [Theory]
    [InlineData("tacos", "taco")]
    [InlineData("Burritos", "burrito")]
    [InlineData("NOODLES", "noodl")]
    public void Stems_singular_and_plural_to_the_same_form(string word, string expectedStem)
    {
        Assert.Equal(expectedStem, FoodTextNormalizer.StemWord(word));
    }
}
