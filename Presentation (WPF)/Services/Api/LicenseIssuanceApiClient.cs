using DVLD.Contracts.LicenseIssuance;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LicenseIssuanceApiClient(
    IApiClient apiClient) : ILicenseIssuanceApiClient
{
    public Task<ApiResult<IssueFirstLicenseResponse>>
        IssueFirstLicenseAsync(
            IssueFirstLicenseRequest request,
            CancellationToken cancellationToken = default)
        => apiClient.PostAsync<
            IssueFirstLicenseRequest,
            IssueFirstLicenseResponse>(
            "api/licenseissuance/first-license",
            request,
            cancellationToken);
}