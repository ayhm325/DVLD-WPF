using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface ILicenseService
    {
        Task<Result<LicenseDto>> GetByIdAsync(int id);

        Task<Result<List<LicenseDto>>> GetAllAsync();

        Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId);

        Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId);

        Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId);

        Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId);

        Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId);

        Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes);

        Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId);

        // CHECKS
        Task<bool> IsLicenseExistsAsync(int id);
        Task<bool> IsDriverHasLicenseAsync(int driverId);
        Task<bool> IsApplicationHasLicenseAsync(int applicationId);

        // COMMANDS
        Task<Result<int>> AddAsync(LicenseDto dto);
        Task<Result> UpdateAsync(LicenseDto dto);
        Task<Result> DeleteAsync(int id);

        Task<Result<int>> RenewLicenseAsync(int oldLicenseId, string? notes);

        Task<Result<int>> ReplaceLicenseAsync(int oldLicenseId, string replacementReason, int applicationTypeId);
    }
}