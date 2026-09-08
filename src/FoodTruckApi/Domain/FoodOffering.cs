namespace FoodTruckApi.Domain;

/// <summary>
/// What a truck serves, derived from the permit's free-text <c>FoodItems</c> field.
/// </summary>
/// <param name="Terms">Normalized, stemmed keywords for the food actually listed.</param>
/// <param name="ServesEverything">
/// True when the listing is a catch-all (e.g. <c>Everything</c>, <c>Multiple Food Trucks &amp; Food Types</c>).
/// </param>
/// <param name="Excludes">
/// Normalized, stemmed keywords a catch-all listing explicitly rules out
/// (e.g. <c>everything except for hot dogs</c> → <c>["dog"]</c>).
/// </param>
public sealed record FoodOffering(
    IReadOnlyList<string> Terms,
    bool ServesEverything,
    IReadOnlyList<string> Excludes)
{
    public static readonly FoodOffering Empty =
        new(Array.Empty<string>(), ServesEverything: false, Array.Empty<string>());
}
