using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDetainedLicenseService
    {
        Task<List<DetainedLicenseDto>> GetAllAsync();

        Task<DetainedLicenseDto?> GetByIdAsync(int id);

        Task<DetainedLicenseDto?> GetActiveDetainByLicenseIdAsync(int licenseId);

        Task<Result<DetainedLicenseDto>> AddAsync(
            DetainedLicenseDto dto);

        Task<Result> UpdateAsync(
            DetainedLicenseDto dto);

        Task<bool> IsLicenseDetainedAsync(
            int licenseId);

        Task<Result> ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId);
    }
}