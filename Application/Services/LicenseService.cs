using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;

namespace Application.Services;

public sealed class LicenseService(
    ILicenseQueryService queryService)
    : ILicenseService
{
    private readonly ILicenseQueryService _queryService =
        queryService
        ?? throw new ArgumentNullException(nameof(queryService));

    public Task<Result<LicenseDto>> GetByIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetByIdAsync(licenseId, cancellationToken);

    public Task<Result<List<LicenseDto>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _queryService.GetAllAsync(cancellationToken);

    public Task<Result<List<LicenseDto>>> GetAllAsync(bool? isExpired, CancellationToken cancellationToken = default) =>
        _queryService.GetAllAsync(isExpired, cancellationToken);

    public Task<Result<PagedResult<LicenseDto>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default) =>
        _queryService.GetAllAsync(request, cancellationToken);

    public Task<Result<PagedResult<LicenseDto>>> GetAllAsync(LicenseQueryRequest request, CancellationToken cancellationToken = default) =>
        _queryService.GetAllAsync(request, cancellationToken);

    public Task<Result<RenewalSummaryDto>> GetRenewalSummaryAsync(CancellationToken cancellationToken = default) =>
        _queryService.GetRenewalSummaryAsync(cancellationToken);

    public Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetByDriverIdAsync(driverId, cancellationToken);

    public Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetByApplicationIdAsync(applicationId, cancellationToken);

    public Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetByLicenseClassIdAsync(licenseClassId, cancellationToken);

    public Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetLicensesByPersonIdAsync(personId, cancellationToken);

    public Task<Result<bool>> IsLicenseExistsAsync(
        int licenseId,
        CancellationToken cancellationToken = default) =>
        _queryService.IsLicenseExistsAsync(licenseId, cancellationToken);

    public Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId,
        CancellationToken cancellationToken = default) =>
        _queryService.IsDriverHasLicenseAsync(driverId, cancellationToken);

    public Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId,
        CancellationToken cancellationToken = default) =>
        _queryService.IsApplicationHasLicenseAsync(applicationId, cancellationToken);

    public Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetDetailsAsync(localAppId, cancellationToken);

    public Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default) =>
        _queryService.GetLicenseDetailsByIdAsync(licenseId, cancellationToken);
}
