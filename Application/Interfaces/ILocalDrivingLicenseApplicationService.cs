using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;

namespace Application.Interfaces;

public interface ILocalDrivingLicenseApplicationService
{
    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync();

    Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(int id);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId);

    Task<Result<decimal>>
        GetNewLocalDrivingLicenseApplicationFeesAsync();

    Task<Result<int>>
        CreateLocalDrivingLicenseApplicationAsync(
            int applicantPersonId,
            int licenseClassId);

    Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto);

    Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto);

    Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(int id);

    Task<Result<int>>
        GetApplicationIdByLocalIdAsync(int localId);

    Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(int id);
}