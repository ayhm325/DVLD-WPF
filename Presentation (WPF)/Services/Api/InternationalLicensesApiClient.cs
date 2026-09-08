using DVLD.Contracts.InternationalLicense;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class InternationalLicensesApiClient(
    IApiClient apiClient) : IInternationalLicensesApiClient
{
    public Task<ApiResult<List<InternationalLicenseResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<InternationalLicenseResponse>>(
            "api/internationallicenses",
            cancellationToken);

    public Task<ApiResult<List<InternationalLicenseResponse>>>
        GetByDriverIdAsync(
            int driverId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<InternationalLicenseResponse>>(
            $"api/internationallicenses/driver/{driverId}",
            cancellationToken);

    public Task<ApiResult<InternationalLicenseResponse>>
        GetByIdAsync(
            int internationalLicenseId,
            CancellationToken cancellationToken = default)
        => apiClient.GetAsync<InternationalLicenseResponse>(
            $"api/internationallicenses/{internationalLicenseId}",
            cancellationToken);
}