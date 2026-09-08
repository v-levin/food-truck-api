using System.Text.RegularExpressions;
using FoodTruckApi.Domain;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// Turns the permit's free-text <c>FoodItems</c> into a <see cref="FoodOffering"/>:
/// positive keywords, plus a "serves everything" flag and any explicit exclusions parsed
/// from phrasing like <c>everything except for hot dogs</c>.
/// </summary>
internal static class FoodOfferingParser
{
    // In this dataset "everything"/"anything" only ever appears as a standalone catch-all,
    // never as "…and everything Mexican", so a plain word match is safe here.
    private static readonly Regex CatchAll = new(
        @"\b(?:everything|anything)\b|\bmultiple\b.*\btypes?\b",
        RegexOptions.IgnoreCase);

    private static readonly Regex Exclusion = new(
        @"\b(?:everything|anything)\b.*?\b(?:except|but|besides|minus)\b\s+(?:for\s+)?(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // Stems of the structural words above; they carry no food meaning of their own.
    private static readonly HashSet<string> StructuralTokens =
        new(StringComparer.Ordinal) { "everyth", "anyth", "except", "besid", "minus" };

    public static FoodOffering Parse(string? foodItems)
    {
        if (string.IsNullOrWhiteSpace(foodItems))
        {
            return FoodOffering.Empty;
        }

        var servesEverything = CatchAll.IsMatch(foodItems);
        IReadOnlyList<string> excludes = Array.Empty<string>();
        var positiveText = foodItems;

        if (servesEverything)
        {
            var exclusion = Exclusion.Match(foodItems);
            if (exclusion.Success)
            {
                excludes = FoodTextNormalizer.ExtractTerms(exclusion.Groups["rest"].Value);
                positiveText = foodItems[..exclusion.Index];
            }
        }

        var terms = FoodTextNormalizer.ExtractTerms(positiveText)
            .Where(term => !StructuralTokens.Contains(term))
            .ToArray();

        return new FoodOffering(terms, servesEverything, excludes);
    }
}
