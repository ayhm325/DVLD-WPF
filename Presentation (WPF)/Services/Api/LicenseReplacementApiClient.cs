using DVLD.Contracts.LicenseReplacement;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class LicenseReplacementApiClient(
    IApiClient apiClient) : ILicenseReplacementApiClient
{
    public Task<ApiResult<ReplaceLicenseResponse>> ReplaceAsync(
        ReplaceLicenseRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PostAsync<
            ReplaceLicenseRequest,
            ReplaceLicenseResponse>(
            "api/LicenseReplacement",
            request,
            cancellationToken);
}