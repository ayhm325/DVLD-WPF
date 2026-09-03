using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;

namespace Application.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseQueryService _queryService;

    public LicenseService(
    ILicenseQueryService queryService)
    {
        _queryService = queryService
            ?? throw new ArgumentNullException(nameof(queryService));
    }

    public Task<Result<LicenseDto>> GetByIdAsync(
        int licenseId)
    {
        return _queryService.GetByIdAsync(licenseId);
    }

    public Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        return _queryService.GetAllAsync();
    }

    public Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId)
    {
        return _queryService.GetByDriverIdAsync(driverId);
    }

    public Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId)
    {
        return _queryService.GetByApplicationIdAsync(
            applicationId);
    }

    public Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId)
    {
        return _queryService.GetByLicenseClassIdAsync(
            licenseClassId);
    }

    public Task<Result<List<LicenseDto>>>
        GetLicensesByPersonIdAsync(int personId)
    {
        return _queryService.GetLicensesByPersonIdAsync(
            personId);
    }

    public Task<Result<bool>> IsLicenseExistsAsync(
        int licenseId)
    {
        return _queryService.IsLicenseExistsAsync(licenseId);
    }

    public Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId)
    {
        return _queryService.IsDriverHasLicenseAsync(
            driverId);
    }

    public Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId)
    {
        return _queryService.IsApplicationHasLicenseAsync(
            applicationId);
    }
}
