using Domain.Entities;

namespace Application.Interfaces;

public interface IInternationalRepository
{
    // =========================================================
    // GET
    // =========================================================

    Task<List<InternationalLicense>>
        GetAllAsync();

    Task<InternationalLicense?>
        GetByIdAsync(
            int internationalLicenseId);

    Task<List<InternationalLicense>>
        GetByDriverIdAsync(
            int driverId);

    Task<InternationalLicense?>
        GetByApplicationIdAsync(
            int applicationId);

    Task<List<InternationalLicense>>
        GetByLocalLicenseIdAsync(
            int localLicenseId);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        ExistsByLocalLicenseAsync(
            int localLicenseId);

    Task<bool>
        HasActiveInternationalLicenseAsync(
            int driverId);


    // =========================================================
    // COMMANDS
    // =========================================================

    Task<int>
        AddAsync(
            InternationalLicense entity);

    Task<bool>
        UpdateAsync(
            InternationalLicense entity);

    Task<bool>
        DeleteAsync(
            int internationalLicenseId);
}