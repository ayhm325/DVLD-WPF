using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ILocalDrivingLicenseApplicationsApiClient
{
    Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetAllAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<LocalDrivingLicenseApplicationResponse>>
        GetByIdAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByApplicationIdAsync(
            int applicationId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByLicenseClassIdAsync(
            int licenseClassId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<List<LocalDrivingLicenseApplicationResponse>>>
        GetByApplicantPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<int>>
        GetApplicationIdAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default);

    Task<ApiResult<int>>
        CreateAsync(
            CreateLocalDrivingLicenseApplicationRequest request,
            CancellationToken cancellationToken = default);

    Task<ApiResult>
        UpdateAsync(
            int localApplicationId,
            UpdateLocalDrivingLicenseApplicationRequest request,
            CancellationToken cancellationToken = default);

    Task<ApiResult>
        DeleteAsync(
            int localApplicationId,
            CancellationToken cancellationToken = default);
}