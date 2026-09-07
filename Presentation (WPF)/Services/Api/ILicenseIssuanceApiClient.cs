using DVLD.Contracts.LicenseIssuance;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILicenseIssuanceApiClient
{
    Task<ApiResult<IssueFirstLicenseResponse>> IssueFirstLicenseAsync(
        IssueFirstLicenseRequest request,
        CancellationToken cancellationToken = default);
}