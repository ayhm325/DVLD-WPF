using DVLD.Contracts.InternationalLicense;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IInternationalLicensesApiClient
{
    Task<ApiResult<List<InternationalLicenseResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<InternationalLicenseResponse>>>
        GetByDriverIdAsync(
            int driverId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<InternationalLicenseResponse>>
        GetByIdAsync(
            int internationalLicenseId,
            CancellationToken cancellationToken = default);
}