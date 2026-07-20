using Domain.Entities;

namespace Application.Interfaces
{
    public interface IDetainedLicenseRepository
    {
        Task<DetainedLicense?> GetByIdAsync(int id);

        Task<List<DetainedLicense>> GetAllAsync();

        Task<DetainedLicense> AddAsync(DetainedLicense entity);

        Task UpdateAsync(DetainedLicense entity);

        Task<bool> IsLicenseDetainedAsync(int licenseId);

        Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(int licenseId);

        Task ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int releaseApplicationId);
    }
}