using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;

namespace Application.Interfaces;

public interface ILocalDrivingLicenseApplicationService
{
    Task<Result<PagedResult<LocalDrivingLicenseApplicationListDto>>>
    GetAllLocalDrivingLicenseApplicationsAsync(
        LocalDrivingLicenseApplicationQueryRequest request, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<LocalDrivingLicenseApplicationListDto>>>
    GetAllLocalDrivingLicenseApplicationsAsync(
        PaginationRequest request, CancellationToken cancellationToken = default);

    Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ApplicationBasicInfoDto>>
        GetApplicationBasicInfoAsync(int localApplicationId, CancellationToken cancellationToken = default);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
            int applicationId, CancellationToken cancellationToken = default);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId, CancellationToken cancellationToken = default);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId, CancellationToken cancellationToken = default);

    Task<Result<decimal>>
        GetNewLocalDrivingLicenseApplicationFeesAsync(CancellationToken cancellationToken = default);

    Task<Result<int>>
        CreateLocalDrivingLicenseApplicationAsync(
            int applicantPersonId,
            int licenseClassId, CancellationToken cancellationToken = default);

    Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto, CancellationToken cancellationToken = default);

    Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto, CancellationToken cancellationToken = default);

    Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(int id, CancellationToken cancellationToken = default);

    Task<Result>
        CancelLocalDrivingLicenseApplicationAsync(
            int localApplicationId, CancellationToken cancellationToken = default);

    Task<Result<int>>
        GetApplicationIdByLocalIdAsync(int localId, CancellationToken cancellationToken = default);

    Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(int id, CancellationToken cancellationToken = default);
}
