using Domain.Entities;

namespace Application.Interfaces;

public interface IDetainedLicenseRepository
{
    // =========================================================
    // GET
    // =========================================================

    Task<DetainedLicense?>
        GetByIdAsync(int id);

    Task<List<DetainedLicense>>
        GetAllAsync();

    Task<DetainedLicense?>
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

    Task<DetainedLicense>
        AddAsync(
            DetainedLicense entity);

    Task
        UpdateAsync(
            DetainedLicense entity);
}