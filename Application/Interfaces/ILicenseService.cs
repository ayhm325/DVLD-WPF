using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.LicenseDTO;

namespace Application.Interfaces;

public interface ILicenseService
{
    Task<Result<LicenseDto>> GetByIdAsync(int licenseId, CancellationToken cancellationToken = default);

    Task<Result<List<LicenseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<List<LicenseDto>>> GetAllAsync(bool? isExpired, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<LicenseDto>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<LicenseDto>>> GetAllAsync(LicenseQueryRequest request, CancellationToken cancellationToken = default);
    Task<Result<RenewalSummaryDto>> GetRenewalSummaryAsync(CancellationToken cancellationToken = default);

    Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId,
        CancellationToken cancellationToken = default);

    Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsLicenseExistsAsync(int licenseId, CancellationToken cancellationToken = default);

    Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId,
        CancellationToken cancellationToken = default);

    Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default);
}
