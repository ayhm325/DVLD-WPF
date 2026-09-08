using DVLD.Contracts.License;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LicensesApiClient(
    IApiClient apiClient) : ILicensesApiClient
{
    public Task<ApiResult<List<LicenseResponse>>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LicenseResponse>>(
            $"api/licenses/application/{applicationId}",
            cancellationToken);

    public Task<ApiResult<LicenseResponse>> GetByIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<LicenseResponse>(
            $"api/licenses/{licenseId}",
            cancellationToken);

    public Task<ApiResult<DriverLicenseInfoResponse>> GetDetailsAsync(
        int localApplicationId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DriverLicenseInfoResponse>(
            $"api/licenses/local-application/{localApplicationId}/details",
            cancellationToken);

    public Task<ApiResult<DriverLicenseInfoResponse>> GetDetailsByIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DriverLicenseInfoResponse>(
            $"api/licenses/{licenseId}/details",
            cancellationToken);

    public Task<ApiResult<List<LicenseResponse>>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<LicenseResponse>>(
            $"api/licenses/driver/{driverId}",
            cancellationToken);
}