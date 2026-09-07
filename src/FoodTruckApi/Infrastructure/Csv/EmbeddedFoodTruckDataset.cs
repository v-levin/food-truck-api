using System.Reflection;

namespace FoodTruckApi.Infrastructure.Csv;

/// <summary>Opens the SF permit CSV that is compiled into this assembly as a resource.</summary>
internal static class EmbeddedFoodTruckDataset
{
    internal const string ResourceName = "FoodTruckApi.Data.Mobile_Food_Facility_Permit.csv";

    public static Stream Open()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded dataset '{ResourceName}' was not found. Available resources: " +
                string.Join(", ", assembly.GetManifestResourceNames()));
    }
}
