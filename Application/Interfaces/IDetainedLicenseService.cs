using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IDetainedLicenseService
    {
        Task<DetainedLicenseDto?> GetByIdAsync(int id);

        Task<List<DetainedLicenseDto>> GetAllAsync();

        Task<DetainedLicense> AddAsync(DetainedLicense entity);

        Task UpdateAsync(DetainedLicense entity);

        Task<bool> IsLicenseDetainedAsync(int licenseId);

        Task ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int applicationId);
    }
}