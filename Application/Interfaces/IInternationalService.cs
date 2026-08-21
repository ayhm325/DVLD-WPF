using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface IInternationalService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<InternationalDto>>>
        GetAllAsync();

    Task<Result<InternationalDto>>
        GetByIdAsync(
            int internationalLicenseId);

    Task<Result<List<InternationalDto>>>
        GetByDriverIdAsync(
            int driverId);

    Task<Result<InternationalDto>>
        GetByApplicationIdAsync(
            int applicationId);

    Task<Result<List<InternationalDto>>>
        GetByLocalLicenseIdAsync(
            int localLicenseId);

    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        HasActiveInternationalLicenseAsync(
            int driverId);

    // =========================================================
    // COMMANDS
    // =========================================================

    Task<Result>
        AddAsync(
            CreateInternationalLicenseDto dto);

    Task<Result>
        UpdateAsync(
            UpdateInternationalLicenseDto dto);

    Task<Result>
        DeleteAsync(
            int internationalLicenseId);

    // =========================================================
    // BUSINESS
    // =========================================================

    Task<Result<int>>
        IssueInternationalLicenseAsync(
            int localLicenseId);

    Task<Result<DriverLicenseInfoDto>>
        GetLocalLicenseInfoAsync(
            int licenseId);
}