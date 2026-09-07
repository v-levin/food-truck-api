using CsvHelper.Configuration;

namespace FoodTruckApi.Infrastructure.Csv;

/// <summary>
/// The handful of raw columns we read from <c>Mobile_Food_Facility_Permit.csv</c>.
/// Everything is a string: coordinates are parsed explicitly so a blank cell becomes
/// "missing" rather than silently binding to 0.
/// </summary>
internal sealed class FoodTruckCsvRecord
{
    public string LocationId { get; init; } = string.Empty;

    public string Applicant { get; init; } = string.Empty;

    public string FacilityType { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string FoodItems { get; init; } = string.Empty;

    public string? Latitude { get; init; }

    public string? Longitude { get; init; }
}

internal sealed class FoodTruckCsvRecordMap : ClassMap<FoodTruckCsvRecord>
{
    public FoodTruckCsvRecordMap()
    {
        Map(m => m.LocationId).Name("locationid");
        Map(m => m.Applicant).Name("Applicant");
        Map(m => m.FacilityType).Name("FacilityType");
        Map(m => m.Address).Name("Address");
        Map(m => m.Status).Name("Status");
        Map(m => m.FoodItems).Name("FoodItems");
        Map(m => m.Latitude).Name("Latitude");
        Map(m => m.Longitude).Name("Longitude");
    }
}
