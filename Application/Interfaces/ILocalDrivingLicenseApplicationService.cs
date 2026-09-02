using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;

namespace Application.Interfaces;

public interface ILocalDrivingLicenseApplicationService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync();

    Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(
            int id);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
            int applicationId);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId);

    Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId);


    // =========================================================
    // COMMANDS
    // =========================================================

    Task<Result<int>> CreateLocalDrivingLicenseApplicationAsync(
    CreateApplicationDto applicationDto,CreateLocalDrivingLicenseApplicationDto localApplicationDto);

    Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto);

    Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto);

    Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(
            int id);


    // =========================================================
    // OTHER
    // =========================================================

    Task<Result<int>>
        GetApplicationIdByLocalIdAsync(
            int localId);

    Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(
            int id);
}