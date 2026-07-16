using Application.DTOs;

namespace Application.Interfaces
{
    public interface ILicenseService
    {
        Task<int> RenewLicenseAsync(int oldLicenseId, string? notes);

        // =========================
        // GET
        // =========================
        Task<LicenseDto?> GetByIdAsync(int id);

        Task<List<LicenseDto>> GetAllAsync();

        Task<List<LicenseDto>> GetByDriverIdAsync(int driverId);

        Task<List<LicenseDto>> GetByApplicationIdAsync(int applicationId);

        Task<List<LicenseDto>> GetByLicenseClassIdAsync(int licenseClassId);

        Task<DriverLicenseInfoDto?> GetDetails(int localAppId);

        Task<DriverLicenseInfoDto?> GetLicenseDetailsByIdAsync(int licenseId);

        Task<int> IssueFirstLicenseAsync(int localAppId, string? notes);

        Task<List<LicenseDto>> GetLicensesByPersonIdAsync(int personId);

        // =========================
        // CHECKS
        // =========================
        Task<bool> IsLicenseExistsAsync(int id);

        Task<bool> IsDriverHasLicenseAsync(int driverId);

        Task<bool> IsApplicationHasLicenseAsync(int applicationId);

        // =========================
        // COMMANDS
        // =========================
        Task<int> AddAsync(LicenseDto dto);

        Task<bool> UpdateAsync(LicenseDto dto);

        Task<bool> DeleteAsync(int id);
    }
}