using Application.DTOs;

namespace Application.Interfaces
{
    public interface IInternationalService
    {
        Task<IEnumerable<InternationalDto>> GetAllAsync();

        Task<InternationalDto?> GetByIdAsync(int internationalLicenseId);

        Task<IEnumerable<InternationalDto>> GetByDriverIdAsync(int driverId);

        Task<InternationalDto?> GetByApplicationIdAsync(int applicationId);

        Task<IEnumerable<InternationalDto>> GetByLocalLicenseIdAsync(int localLicenseId);

        Task<bool> HasActiveInternationalLicenseAsync(int driverId);

        Task AddAsync(InternationalDto dto);

        Task UpdateAsync(InternationalDto dto);

        Task DeleteAsync(int internationalLicenseId);
    }
}