using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;

namespace Application.Interfaces;

public interface IDetainedLicenseService
{
    Task<Result<List<DetainedLicenseDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<DetainedLicenseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<DetainedLicenseDto>> GetActiveDetainByLicenseIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default);

    Task<bool> IsLicenseDetainedAsync(int licenseId, CancellationToken cancellationToken = default);

    Task<Result<DetainedLicenseDto>> AddAsync(
        CreateDetainedLicenseDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> ReleaseAsync(
        ReleaseDetainedLicenseDto dto,
        CancellationToken cancellationToken = default);
}