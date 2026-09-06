using Domain.Entities;

namespace Application.Interfaces;

public interface ILicenseRepository
{
    Task<License?> GetLicenseByIdAsync(int id);
    Task<License?> GetByDriverIdAsync(int driverId);

    Task<List<License>> GetAllLicensesAsync();
    Task<List<License>> GetLicensesByDriverIdAsync(int driverId);
    Task<List<License>> GetLicensesByApplicationIdAsync(
        int applicationId);
    Task<List<License>> GetLicensesByLicenseClassIdAsync(
        int licenseClassId);
    Task<List<License>> GetLicensesByPersonIdAsync(int personId);

    Task<bool> IsLicenseExistsAsync(int id);
    Task<bool> IsDriverHasLicenseAsync(int driverId);
    Task<bool> IsApplicationHasLicenseAsync(int applicationId);
    Task<bool> IsActiveLicenseExistsAsync(
        int driverId,
        int licenseClassId);

    Task<HashSet<int>> GetApplicationIdsWithLicensesAsync(
        IEnumerable<int> applicationIds);

    Task AddLicenseAsync(License license);
    Task<bool> DeactivateLicenseAsync(int licenseId);
    Task<bool> DeleteLicenseAsync(int id);

    Task<bool> HasAnotherActiveLicenseAsync(
        int driverId,
        int licenseClassId,
        int excludedLicenseId);

    Task<bool> ActivateLicenseAsync(int licenseId);
}