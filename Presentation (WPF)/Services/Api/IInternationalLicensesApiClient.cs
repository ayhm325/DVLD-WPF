using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IInternationalLicensesApiClient
{
    Task<ApiResult<List<InternationalLicenseListResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<InternationalLicenseListResponse>>>
        GetByDriverIdAsync(
            int driverId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<InternationalLicenseResponse>>
        GetByIdAsync(
            int internationalLicenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<InternationalLicenseListResponse>>>
        GetByLocalLicenseIdAsync(
            int localLicenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<DriverLicenseInfoResponse>>
        GetLocalLicenseInfoAsync(
            int licenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<InternationalLicenseResponse>>
        IssueAsync(
            IssueInternationalLicenseRequest request,
            CancellationToken cancellationToken = default);
}