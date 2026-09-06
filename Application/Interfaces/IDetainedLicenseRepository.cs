using Domain.Entities;

namespace Application.Interfaces;

public interface IDetainedLicenseRepository
{
    Task<List<DetainedLicense>> GetAllAsync();

    Task<DetainedLicense?> GetByIdAsync(int id);

    Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(
        int licenseId);

    Task<bool> IsLicenseDetainedAsync(int licenseId);

    Task AddAsync(DetainedLicense entity);

    Task<DetainedLicense?> GetByIdForUpdateAsync(int id);
}