using Application.Common.Pagination;
using Application.DTOs.LicenseDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface ILicenseRepository
{
    Task<License?> GetLicenseByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<License?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<License?> GetByDriverIdAsync(int driverId, CancellationToken cancellationToken = default);

    Task<List<License>> GetAllLicensesAsync(CancellationToken cancellationToken = default);
    Task<List<License>> GetAllLicensesAsync(bool? isExpired, DateOnly? jordanToday = null, CancellationToken cancellationToken = default);
    Task<PagedResult<License>> GetAllLicensesAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<License>> GetAllLicensesAsync(LicenseQueryRequest request, DateOnly? jordanToday = null, CancellationToken cancellationToken = default);

    Task<List<License>> GetLicensesByDriverIdAsync(int driverId, CancellationToken cancellationToken = default);
    Task<List<License>> GetLicensesByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default);
    Task<List<License>> GetLicensesByLicenseClassIdAsync(
        int licenseClassId,
        CancellationToken cancellationToken = default);
    Task<List<License>> GetLicensesByPersonIdAsync(int personId, CancellationToken cancellationToken = default);

    Task<bool> IsLicenseExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsDriverHasLicenseAsync(int driverId, CancellationToken cancellationToken = default);
    Task<bool> IsApplicationHasLicenseAsync(int applicationId, CancellationToken cancellationToken = default);

    Task<bool> IsActiveLicenseExistsAsync(
        int driverId,
        int licenseClassId,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetApplicationIdsWithLicensesAsync(
        IEnumerable<int> applicationIds,
        CancellationToken cancellationToken = default);

    Task AddLicenseAsync(License license, CancellationToken cancellationToken = default);

    Task<bool> DeactivateLicenseAsync(int licenseId, CancellationToken cancellationToken = default);

    Task<bool> HasAnotherActiveLicenseAsync(
        int driverId,
        int licenseClassId,
        int excludedLicenseId,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateLicenseAsync(int licenseId, DateOnly jordanToday, CancellationToken cancellationToken = default);

    Task<int> DeactivateExpiredLicensesAsync(DateOnly jordanToday, CancellationToken cancellationToken = default);

    Task<RenewalSummaryDto> GetRenewalSummaryAsync(
        DateOnly jordanToday,
        CancellationToken cancellationToken = default);

    Task<License?> GetNewerLicenseForDriverAndClassAsync(
        int driverId,
        int licenseClassId,
        int licenseId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, License>> GetNewerLicensesBatchAsync(
        IEnumerable<License> licenses,
        CancellationToken cancellationToken = default);
}
