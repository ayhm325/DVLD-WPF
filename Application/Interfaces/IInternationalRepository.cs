using Domain.Entities;

namespace Application.Interfaces;

public interface IInternationalRepository
{
    Task<List<InternationalLicense>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<InternationalLicense?> GetByIdAsync(
        int internationalLicenseId,
        CancellationToken cancellationToken = default);

    Task<List<InternationalLicense>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<InternationalLicense?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<List<InternationalLicense>> GetByLocalLicenseIdAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByLocalLicenseAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveInternationalLicenseAsync(
        int driverId,
        DateOnly jordanToday,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        InternationalLicense entity,
        CancellationToken cancellationToken = default);
}
