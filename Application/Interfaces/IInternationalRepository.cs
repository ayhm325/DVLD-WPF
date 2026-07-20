using Domain.Entities;

namespace Application.Interfaces
{
    public interface IInternationalRepository
    {
        Task<IEnumerable<InternationalLicense>> GetAllAsync();

        Task<InternationalLicense?> GetByIdAsync(int id);

        Task<IEnumerable<InternationalLicense>> GetByDriverIdAsync(int driverId);

        Task<InternationalLicense?> GetByApplicationIdAsync(int applicationId);

        Task<IEnumerable<InternationalLicense>> GetByLocalLicenseIdAsync(int localLicenseId);


        Task<bool> ExistsByLocalLicenseAsync(int localLicenseId);

        Task<bool> HasActiveInternationalLicenseAsync(int driverId);


        Task AddAsync(InternationalLicense entity);

        Task UpdateAsync(InternationalLicense entity);

        Task DeleteAsync(int id);
    }
}