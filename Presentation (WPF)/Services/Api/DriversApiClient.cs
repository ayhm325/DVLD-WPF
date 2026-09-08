using DVLD.Contracts.Driver;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class DriversApiClient(
    IApiClient apiClient) : IDriversApiClient
{
    public Task<ApiResult<IReadOnlyList<DriverResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<IReadOnlyList<DriverResponse>>(
            "api/drivers",
            cancellationToken);

    public Task<ApiResult<DriverResponse>>
        GetByPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DriverResponse>(
            $"api/drivers/person/{personId}",
            cancellationToken);
}