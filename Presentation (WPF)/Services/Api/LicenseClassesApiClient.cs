using DVLD.Contracts.LicenseClass;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LicenseClassesApiClient(
    IApiClient apiClient) : ILicenseClassesApiClient
{
    public Task<ApiResult<List<LicenseClassResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<LicenseClassResponse>>(
            "api/licenseclasses",
            cancellationToken);
    }

    public Task<ApiResult<LicenseClassResponse>> GetByIdAsync(
        int licenseClassId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<LicenseClassResponse>(
            $"api/licenseclasses/{licenseClassId}",
            cancellationToken);
    }
}