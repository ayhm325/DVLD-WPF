using DVLD.Contracts.Driver;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class DriversApiClient(
    IApiClient apiClient) : IDriversApiClient
{
    public Task<ApiResult<IReadOnlyList<DriverListResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<IReadOnlyList<DriverListResponse>>(
            "api/drivers",
            cancellationToken);

    public Task<ApiResult<DriverResponse>>
        GetByPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DriverResponse>(
            $"api/drivers/person/{personId}",
            cancellationToken);

    public async Task<ApiResult<int>> CreateAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PostAsync<CreateDriverRequest, DriverResponse>(
            "api/drivers",
            request,
            cancellationToken);

        if (result.IsFailure)
            return ApiResult<int>.Failure(result.Error);

        return ApiResult<int>.Success(result.Value!.DriverId);
    }

    public Task<ApiResult> DeleteAsync(
        int driverId,
        CancellationToken cancellationToken = default) =>
        apiClient.DeleteAsync($"api/drivers/{driverId}", cancellationToken);
}
