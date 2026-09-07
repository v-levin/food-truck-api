using System.Text;
using FoodTruckApi.Infrastructure.Csv;

namespace FoodTruckApi.Tests.Infrastructure.Csv;

public class FoodTruckCsvLoaderTests
{
    private const string Header =
        "locationid,Applicant,FacilityType,cnn,LocationDescription,Address,blocklot,block,lot," +
        "permit,Status,FoodItems,X,Y,Latitude,Longitude,Schedule,dayshours,NOISent,Approved," +
        "Received,PriorPermit,ExpirationDate,Location";

    [Fact]
    public void Load_keeps_only_approved_located_trucks_from_the_embedded_dataset()
    {
        using var stream = EmbeddedFoodTruckDataset.Open();

        var result = FoodTruckCsvLoader.Load(stream);

        Assert.Equal(158, result.Trucks.Count);
        Assert.True(result.SkippedRows > 0);
        Assert.All(result.Trucks, truck =>
        {
            Assert.Equal("Truck", truck.FacilityType);
            Assert.NotNull(truck.Location);
            Assert.InRange(truck.Location.Latitude, 37.6, 37.9);
            Assert.InRange(truck.Location.Longitude, -122.6, -122.3);
        });
    }

    [Theory]
    [InlineData("Push Cart", "APPROVED", "37.77", "-122.42")]
    [InlineData("Truck", "REQUESTED", "37.77", "-122.42")]
    [InlineData("Truck", "EXPIRED", "37.77", "-122.42")]
    [InlineData("Truck", "APPROVED", "", "")]
    [InlineData("Truck", "APPROVED", "0", "0")]
    [InlineData("Truck", "APPROVED", "not-a-number", "-122.42")]
    public void Load_skips_rows_that_fail_a_static_filter(
        string facilityType, string status, string latitude, string longitude)
    {
        var csv = BuildCsv(
            Row(facilityType: facilityType, status: status, latitude: latitude, longitude: longitude),
            Row(facilityType: "Truck", status: "APPROVED", latitude: "37.78", longitude: "-122.41"));

        var result = FoodTruckCsvLoader.Load(csv);

        Assert.Single(result.Trucks);
        Assert.Equal(1, result.SkippedRows);
    }

    [Fact]
    public void Load_maps_the_fields_we_expose()
    {
        var csv = BuildCsv(Row(
            locationId: "12345",
            applicant: "Tacos El Primo",
            facilityType: "Truck",
            address: "123 MISSION ST",
            status: "APPROVED",
            foodItems: "Tacos: Burritos",
            latitude: "37.7601",
            longitude: "-122.4188"));

        var truck = Assert.Single(FoodTruckCsvLoader.Load(csv).Trucks);

        Assert.Equal("12345", truck.Id);
        Assert.Equal("Tacos El Primo", truck.Name);
        Assert.Equal("123 MISSION ST", truck.Address);
        Assert.Equal("Tacos: Burritos", truck.FoodItems);
        Assert.Equal(37.7601, truck.Location.Latitude);
        Assert.Equal(-122.4188, truck.Location.Longitude);
    }

    [Fact]
    public void Load_preserves_commas_inside_quoted_food_items()
    {
        var csv = BuildCsv(Row(
            facilityType: "Truck",
            status: "APPROVED",
            foodItems: "\"Tacos, Burritos, and Quesadillas\"",
            latitude: "37.78",
            longitude: "-122.41"));

        var truck = Assert.Single(FoodTruckCsvLoader.Load(csv).Trucks);

        Assert.Equal("Tacos, Burritos, and Quesadillas", truck.FoodItems);
    }

    private static Stream BuildCsv(params string[] rows)
    {
        var content = new StringBuilder(Header).Append('\n');
        foreach (var row in rows)
        {
            content.Append(row).Append('\n');
        }

        return new MemoryStream(Encoding.UTF8.GetBytes(content.ToString()));
    }

    private static string Row(
        string locationId = "1",
        string applicant = "Test Truck",
        string facilityType = "Truck",
        string address = "1 TEST ST",
        string status = "APPROVED",
        string foodItems = "Tacos",
        string latitude = "37.77",
        string longitude = "-122.42")
    {
        // 24 columns; only the ones the loader reads carry meaningful values.
        return string.Join(',', new[]
        {
            locationId, applicant, facilityType, "0", "TEST LOCATION", address, "0", "0", "0",
            "TESTPERMIT", status, foodItems, "0", "0", latitude, longitude, "", "", "", "",
            "", "0", "", "",
        });
    }
}
