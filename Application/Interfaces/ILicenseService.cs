using Application.Common.Results;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface ILicenseService
{
    // GET
    Task<Result<LicenseDto>> GetByIdAsync(int id);
    Task<Result<List<LicenseDto>>> GetAllAsync();
    Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId);
    Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId);
    Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId);
    Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId);
    Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId);
    Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId);

    // Business Operations
    Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes);
    Task<Result<int>> RenewLicenseAsync(int oldLicenseId, string? notes);
    Task<Result<int>> ReplaceLicenseAsync(int oldLicenseId, string replacementReason, int applicationTypeId);

    // Checks
    Task<bool> IsLicenseExistsAsync(int id);
    Task<bool> IsDriverHasLicenseAsync(int driverId);
    Task<bool> IsApplicationHasLicenseAsync(int applicationId);

    // Commands
    Task<Result<int>> AddAsync(CreateLicenseDto dto);
    Task<Result> UpdateAsync(UpdateLicenseDto dto);
    Task<Result> DeleteAsync(int id);
}