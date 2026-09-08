using DVLD.Contracts.LicenseRenewal;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LicenseRenewalApiClient(
    IApiClient apiClient) : ILicenseRenewalApiClient
{
    public Task<ApiResult<RenewLicenseResponse>> RenewAsync(
        RenewLicenseRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PostAsync<
            RenewLicenseRequest,
            RenewLicenseResponse>(
            "api/LicenseRenewal",
            request,
            cancellationToken);
}