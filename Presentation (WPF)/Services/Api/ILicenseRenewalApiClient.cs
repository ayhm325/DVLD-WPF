using DVLD.Contracts.LicenseRenewal;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILicenseRenewalApiClient
{
    Task<ApiResult<RenewLicenseResponse>> RenewAsync(
        RenewLicenseRequest request,
        CancellationToken cancellationToken = default);
}