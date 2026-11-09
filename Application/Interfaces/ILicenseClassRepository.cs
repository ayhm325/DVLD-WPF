using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILicenseClassRepository
    {
        Task<List<LicenseClass>> GetAllLicenseClassAsync(CancellationToken cancellationToken = default);

        Task<LicenseClass?> GetLicenseClassByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<LicenseClass?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    }
}
