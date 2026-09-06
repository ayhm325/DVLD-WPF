using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;

namespace Application.Interfaces;

public interface IDetainedLicenseService
{
    Task<Result<List<DetainedLicenseDto>>> GetAllAsync();

    Task<Result<DetainedLicenseDto>> GetByIdAsync(int id);

    Task<Result<DetainedLicenseDto>> GetActiveDetainByLicenseIdAsync(
        int licenseId);

    Task<bool> IsLicenseDetainedAsync(int licenseId);

    Task<Result<DetainedLicenseDto>> AddAsync(
        CreateDetainedLicenseDto dto);

    Task<Result> ReleaseAsync(
        ReleaseDetainedLicenseDto dto);
}