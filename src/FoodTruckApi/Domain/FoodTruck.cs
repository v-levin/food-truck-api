namespace FoodTruckApi.Domain;

/// <summary>
/// An approved, located food truck from the San Francisco permit dataset.
/// One row represents a single permitted pitch, so the same operator can appear
/// multiple times at different locations.
/// </summary>
/// <param name="Id">The dataset <c>locationid</c>.</param>
/// <param name="Name">The permit holder (<c>Applicant</c>).</param>
/// <param name="FacilityType">Always <c>Truck</c> after filtering; kept for transparency in responses.</param>
/// <param name="Address">Street address of the pitch.</param>
/// <param name="Location">Validated coordinates of the pitch.</param>
/// <param name="FoodItems">Raw, free-text list of food served (colon-delimited in the source).</param>
public sealed record FoodTruck(
    string Id,
    string Name,
    string FacilityType,
    string Address,
    Coordinate Location,
    string FoodItems)
{
    /// <summary>
    /// Structured view of <see cref="FoodItems"/> used for matching. Populated once when
    /// the dataset is loaded; <see cref="FoodOffering.Empty"/> until then.
    /// </summary>
    public FoodOffering Offering { get; init; } = FoodOffering.Empty;
}
