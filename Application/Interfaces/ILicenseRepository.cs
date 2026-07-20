using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILicenseRepository
    {
        Task<License?> GetLicenseByIdAsync(int id);

        Task<License?> GetByDriverIdAsync(int driverId);

        Task<List<License>> GetAllLicensesAsync();

        Task<List<License>> GetLicensesByDriverIdAsync(int driverId);

        Task<List<License>> GetLicensesByApplicationIdAsync(int applicationId);

        Task<List<License>> GetLicensesByLicenseClassIdAsync(int licenseClassId);

        Task<List<License>> GetLicensesByPersonIdAsync(int personId);


        Task<bool> IsLicenseExistsAsync(int id);

        Task<bool> IsDriverHasLicenseAsync(int driverId);

        Task<bool> IsApplicationHasLicenseAsync(int applicationId);


        Task<int> AddLicenseAsync(License license);

        Task<bool> UpdateLicenseAsync(License license);

        Task<bool> DeleteLicenseAsync(int id);
    }
}