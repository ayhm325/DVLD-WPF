using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILicenseClassRepository
    {
        Task<List<LicenseClass>> GetAllLicenseClassAsync();

        Task<LicenseClass?> GetLicenseClassByIdAsync(int id);
    }
}