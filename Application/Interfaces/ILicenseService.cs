using Application.Common.Results;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface ILicenseService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<LicenseDto>> GetByIdAsync(int id);

    Task<Result<List<LicenseDto>>> GetAllAsync();

    Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId);

    Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId);

    Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId);

    Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(
        int personId);

    Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId);

    Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(
        int licenseId);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<Result<bool>> IsLicenseExistsAsync(
        int id);

    Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId);

    Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId);


    // =========================================================
    // CRUD
    // =========================================================

    Task<Result<int>> AddAsync(
        CreateLicenseDto dto);

    Task<Result> UpdateAsync(
        UpdateLicenseDto dto);

    Task<Result> DeleteAsync(
        int id);
}