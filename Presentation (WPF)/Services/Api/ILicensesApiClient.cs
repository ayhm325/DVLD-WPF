using DVLD.Contracts.License;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILicensesApiClient
{
    Task<ApiResult<List<LicenseResponse>>>
        GetByApplicationIdAsync(
            int applicationId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<LicenseResponse>>
        GetByIdAsync(
            int licenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<DriverLicenseInfoResponse>>
        GetDetailsByIdAsync(
            int licenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<LicenseResponse>>>
        GetByDriverIdAsync(
            int driverId,
            CancellationToken cancellationToken = default);
}