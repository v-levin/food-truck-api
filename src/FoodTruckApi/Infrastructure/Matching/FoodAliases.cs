namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// A small hand-curated set of food synonyms and cuisine → dish groupings. Two stemmed
/// terms are "related" when they share a group, which lets a query like <c>bbq</c> match
/// <c>barbecue</c>, or <c>mexican</c> match a taco truck, without relying on fuzzy string
/// similarity (which cannot bridge those) or the embedding model (which is weak on short
/// terms). Every entry is a distinctive food word — generic tokens ("meat", "rice") are
/// kept out so they cannot pull unrelated trucks into a match.
/// </summary>
internal static class FoodAliases
{
    private static readonly string[][] Groups =
    {
        new[] { "bbq", "barbecue", "barbeque", "brisket" },
        new[] { "sandwich", "sub", "hoagie", "grinder", "panini" },
        new[] { "burger", "hamburger", "cheeseburger" },
        new[] { "fries", "frites" },
        new[] { "shrimp", "prawn" },
        new[] { "hotdog", "corndog", "frankfurter", "bratwurst", "kielbasa" },
        new[] { "mexican", "taco", "burrito", "quesadilla", "torta", "pupusa", "tamale", "nachos" },
        new[] { "chinese", "wonton", "dumpling", "eggroll" },
        new[] { "japanese", "sushi", "sashimi", "ramen", "teriyaki", "tempura" },
        new[] { "vietnamese", "pho" },
        new[] { "thai", "satay" },
        new[] { "korean", "kimchi", "bulgogi", "bibimbap" },
        new[] { "indian", "naan", "samosa", "biryani" },
        new[] { "dessert", "cake", "cupcake", "cookie", "brownie", "pastry", "donut", "gelato" },
        new[] { "coffee", "espresso", "latte", "cappuccino", "macchiato" },
        new[] { "salad", "greens" },
    };

    private static readonly Dictionary<string, HashSet<int>> GroupsByTerm = BuildIndex();

    /// <summary>True when the two stemmed terms are the same or share an alias group.</summary>
    public static bool Related(string stemmedA, string stemmedB) =>
        stemmedA == stemmedB
        || (GroupsByTerm.TryGetValue(stemmedA, out var a)
            && GroupsByTerm.TryGetValue(stemmedB, out var b)
            && a.Overlaps(b));

    private static Dictionary<string, HashSet<int>> BuildIndex()
    {
        var index = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);

        for (var groupId = 0; groupId < Groups.Length; groupId++)
        {
            foreach (var word in Groups[groupId])
            {
                var stem = FoodTextNormalizer.StemWord(word);
                if (stem.Length < 3)
                {
                    continue;
                }

                if (!index.TryGetValue(stem, out var groupIds))
                {
                    index[stem] = groupIds = new HashSet<int>();
                }

                groupIds.Add(groupId);
            }
        }

        return index;
    }
}
