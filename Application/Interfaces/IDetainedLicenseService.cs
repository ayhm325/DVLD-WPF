using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;

namespace Application.Interfaces;

public interface IDetainedLicenseService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<DetainedLicenseDto>>>
        GetAllAsync();

    Task<Result<DetainedLicenseDto>>
        GetByIdAsync(int id);

    Task<Result<DetainedLicenseDto>>
        GetActiveDetainByLicenseIdAsync(
            int licenseId);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        IsLicenseDetainedAsync(
            int licenseId);


    // =========================================================
    // COMMANDS
    // =========================================================

    Task<Result<DetainedLicenseDto>>
        AddAsync(
            CreateDetainedLicenseDto dto);

    Task<Result>
        UpdateAsync(
            UpdateDetainedLicenseDto dto);

    Task<Result>
        ReleaseAsync(
            ReleaseDetainedLicenseDto dto);
}