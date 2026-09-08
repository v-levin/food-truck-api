using FoodTruckApi.Application.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodTruckApi.Api.Health;

/// <summary>
/// Reports the service healthy only if the food truck dataset loaded and holds at least
/// one truck. Startup already fails fast on an empty dataset, so in practice this is the
/// conventional probe endpoint plus a guard for any future non-embedded data source.
/// </summary>
internal sealed class DatasetHealthCheck : IHealthCheck
{
    private readonly IFoodTruckRepository _repository;

    public DatasetHealthCheck(IFoodTruckRepository repository) => _repository = repository;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var count = _repository.GetAll().Count;

        return Task.FromResult(count > 0
            ? HealthCheckResult.Healthy($"{count} food trucks loaded.")
            : HealthCheckResult.Unhealthy("The food truck dataset is empty."));
    }
}
