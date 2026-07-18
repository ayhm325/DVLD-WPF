using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IDetainedLicenseService
    {
        Task<DetainedLicenseDto?> GetByIdAsync(int id);

        Task<List<DetainedLicenseDto>> GetAllAsync();

        Task<DetainedLicenseDto?> AddAsync(DetainedLicenseDto dto);

        Task UpdateAsync(DetainedLicenseDto dto);

        Task<DetainedLicenseDto?> GetActiveDetainByLicenseIdAsync(int licenseId);

        Task<bool> IsLicenseDetainedAsync(int licenseId);

        Task ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId);
    }
}