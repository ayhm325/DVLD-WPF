using Application.Common.Results;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface ILicenseService
{
    Task<Result<LicenseDto>> GetByIdAsync(int licenseId);


    Task<Result<List<LicenseDto>>> GetAllAsync();

    Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId);

    Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId);

    Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId);

    Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(
        int personId);

    Task<Result<bool>> IsLicenseExistsAsync(int licenseId);

    Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId);

    Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId);

}
