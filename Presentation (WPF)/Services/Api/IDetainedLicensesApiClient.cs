using DVLD.Contracts.DetainedLicense;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IDetainedLicensesApiClient
{
    Task<ApiResult<List<DetainedLicenseResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default);

    Task<ApiResult<DetainedLicenseResponse>>
        GetByIdAsync(
            int detainId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<DetainedLicenseResponse>>
        GetActiveByLicenseIdAsync(
            int licenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<bool>>
        IsLicenseDetainedAsync(
            int licenseId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<DetainedLicenseResponse>>
        DetainAsync(
            CreateDetainedLicenseRequest request,
            CancellationToken cancellationToken = default);

    Task<ApiResult>
        ReleaseAsync(
            ReleaseDetainedLicenseRequest request,
            CancellationToken cancellationToken = default);
}