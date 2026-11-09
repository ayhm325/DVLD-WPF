using Application.Common.Pagination;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface ILocalDrivingLicenseApplicationRepository
{
    Task<PagedResult<LocalDrivingLicenseApplication>> GetAllAsync(
        LocalDrivingLicenseApplicationQueryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<LocalDrivingLicenseApplication>> GetAllAsync(
        PaginationRequest request, CancellationToken cancellationToken = default);

    Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<LocalDrivingLicenseApplication?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(
        int personId, CancellationToken cancellationToken = default);

    Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(
        int applicationId, CancellationToken cancellationToken = default);

    Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(
        int licenseClassId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, int>> GetPassedTestCountsAsync(
        IEnumerable<int> localApplicationIds, CancellationToken cancellationToken = default);

    Task<int?> GetApplicationIdByLocalIdAsync(int localId, CancellationToken cancellationToken = default);

    Task<int?> HasDuplicateApplicationAsync(
        int applicantPersonId,
        int licenseClassId, CancellationToken cancellationToken = default);

    Task AddAsync(LocalDrivingLicenseApplication entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
