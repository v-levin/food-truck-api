using System.Globalization;
using System.Text;
using Porter2Stemmer;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// Shared text pipeline so the food description in the dataset and the caller's query are
/// reduced the same way before they are compared: lower-case, strip punctuation, split on
/// the dataset's delimiters, drop noise words, and stem each token (so "tacos" and "taco"
/// collapse to one form).
/// </summary>
internal static class FoodTextNormalizer
{
    private static readonly EnglishPorter2Stemmer Stemmer = new();

    // Separators seen in the dataset's FoodItems column, plus common conjunctions.
    private static readonly char[] TermSeparators = { ':', ';', ',', '&', '/', '|', '.', '(', ')', '\n', '\r', '\t' };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "and", "or", "the", "a", "an", "of", "with", "in", "on", "to", "for",
        "various", "assorted", "multiple", "misc", "miscellaneous", "etc",
        "food", "foods", "item", "items", "truck", "trucks", "menu", "served", "serving",
        "hot", "cold", "fresh", "homemade", "prepackaged", "pre", "packaged", "premade",
    };

    /// <summary>Normalized, stemmed keywords for a dataset food description.</summary>
    public static IReadOnlyList<string> ExtractTerms(string? foodItems)
    {
        if (string.IsNullOrWhiteSpace(foodItems))
        {
            return Array.Empty<string>();
        }

        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var segment in foodItems.Split(TermSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var token in Tokenize(segment))
            {
                if (seen.Add(token))
                {
                    terms.Add(token);
                }
            }
        }

        return terms;
    }

    /// <summary>Normalized, stemmed keywords for a caller's free-text query.</summary>
    public static IReadOnlyList<string> ExtractQueryTerms(string? query) => ExtractTerms(query);

    /// <summary>Splits a phrase into normalized, stemmed, non-noise tokens.</summary>
    private static IEnumerable<string> Tokenize(string segment)
    {
        foreach (var rawWord in segment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var word = Normalize(rawWord);
            if (word.Length < 2 || StopWords.Contains(word))
            {
                continue;
            }

            var stem = Stemmer.Stem(word).Value;
            if (stem.Length >= 2 && !StopWords.Contains(stem))
            {
                yield return stem;
            }
        }
    }

    /// <summary>Lower-cases and removes everything that is not a letter or digit.</summary>
    private static string Normalize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    // Kept internal-only helper visible for targeted tests.
    internal static string NormalizeWord(string value) => Normalize(value);

    internal static string StemWord(string value) =>
        Stemmer.Stem(Normalize(value)).Value;

    internal static CultureInfo Culture => CultureInfo.InvariantCulture;
}
