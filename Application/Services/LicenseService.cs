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
        int licenseId) =>
        _queryService.GetByIdAsync(licenseId);

    public Task<Result<List<LicenseDto>>> GetAllAsync() =>
        _queryService.GetAllAsync();

    public Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId) =>
        _queryService.GetByDriverIdAsync(driverId);

    public Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId) =>
        _queryService.GetByApplicationIdAsync(applicationId);

    public Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId) =>
        _queryService.GetByLicenseClassIdAsync(licenseClassId);

    public Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(
        int personId) =>
        _queryService.GetLicensesByPersonIdAsync(personId);

    public Task<Result<bool>> IsLicenseExistsAsync(
        int licenseId) =>
        _queryService.IsLicenseExistsAsync(licenseId);

    public Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId) =>
        _queryService.IsDriverHasLicenseAsync(driverId);

    public Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId) =>
        _queryService.IsApplicationHasLicenseAsync(applicationId);

    public Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId) =>
        _queryService.GetDetailsAsync(localAppId);

    public Task<Result<DriverLicenseInfoDto>>
        GetLicenseDetailsByIdAsync(
            int licenseId) =>
        _queryService.GetLicenseDetailsByIdAsync(licenseId);
}