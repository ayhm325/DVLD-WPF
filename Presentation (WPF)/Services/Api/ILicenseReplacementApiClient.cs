using DVLD.Contracts.LicenseReplacement;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILicenseReplacementApiClient
{
    Task<ApiResult<ReplaceLicenseResponse>> ReplaceAsync(
        ReplaceLicenseRequest request,
        CancellationToken cancellationToken = default);
}