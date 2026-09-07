using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FoodTruckApi.Domain;

namespace FoodTruckApi.Infrastructure.Csv;

/// <summary>Outcome of parsing the dataset: the trucks we kept and how many rows we dropped.</summary>
internal sealed record FoodTruckLoadResult(IReadOnlyList<FoodTruck> Trucks, int SkippedRows);

/// <summary>
/// Parses <c>Mobile_Food_Facility_Permit.csv</c> and applies the static domain filters:
/// keep a row only if it is an <c>APPROVED</c> <c>Truck</c> with a usable location.
/// Push carts, non-approved permits and rows without coordinates are dropped and counted.
/// </summary>
internal static class FoodTruckCsvLoader
{
    private const string TruckFacilityType = "Truck";
    private const string ApprovedStatus = "APPROVED";

    public static FoodTruckLoadResult Load(Stream csv)
    {
        using var reader = new StreamReader(csv);
        using var csvReader = new CsvReader(reader, CreateConfiguration());
        csvReader.Context.RegisterClassMap<FoodTruckCsvRecordMap>();

        var trucks = new List<FoodTruck>();
        var skipped = 0;

        foreach (var record in csvReader.GetRecords<FoodTruckCsvRecord>())
        {
            var truck = TryMap(record);
            if (truck is null)
            {
                skipped++;
                continue;
            }

            trucks.Add(truck);
        }

        return new FoodTruckLoadResult(trucks, skipped);
    }

    private static CsvConfiguration CreateConfiguration() => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        TrimOptions = TrimOptions.Trim,
        // The dataset has ragged rows and odd quoting; we validate every field ourselves
        // below, so tell CsvHelper not to throw on either.
        MissingFieldFound = null,
        BadDataFound = null,
    };

    private static FoodTruck? TryMap(FoodTruckCsvRecord record)
    {
        if (!string.Equals(record.FacilityType, TruckFacilityType, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!string.Equals(record.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryParseCoordinatePart(record.Latitude, out var latitude) ||
            !TryParseCoordinatePart(record.Longitude, out var longitude))
        {
            return null;
        }

        // SF is nowhere near the equator or prime meridian, so an exact zero on either
        // axis is the dataset's way of saying "location unknown".
        if (latitude == 0d || longitude == 0d)
        {
            return null;
        }

        var location = Coordinate.Create(latitude, longitude);
        if (location.IsFailure)
        {
            return null;
        }

        return new FoodTruck(
            Id: record.LocationId,
            Name: record.Applicant,
            FacilityType: record.FacilityType,
            Address: record.Address,
            Location: location.Value,
            FoodItems: record.FoodItems);
    }

    private static bool TryParseCoordinatePart(string? raw, out double value) =>
        double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && !double.IsNaN(value)
        && !double.IsInfinity(value);
}
