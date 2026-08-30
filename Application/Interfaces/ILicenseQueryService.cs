using Application.Common.Results;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface ILicenseQueryService
{
    Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId);

    Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(
        int licenseId);
}