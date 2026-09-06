using Domain.Entities;

namespace Application.Interfaces;

public interface IInternationalRepository
{
    Task<List<InternationalLicense>> GetAllAsync();

    Task<InternationalLicense?> GetByIdAsync(
    int internationalLicenseId);

    Task<List<InternationalLicense>> GetByDriverIdAsync(
        int driverId);

    Task<InternationalLicense?> GetByApplicationIdAsync(
        int applicationId);

    Task<List<InternationalLicense>> GetByLocalLicenseIdAsync(
        int localLicenseId);

    Task<bool> ExistsByLocalLicenseAsync(
        int localLicenseId);

    Task<bool> HasActiveInternationalLicenseAsync(
        int driverId);

    Task AddAsync(
        InternationalLicense entity);
}
