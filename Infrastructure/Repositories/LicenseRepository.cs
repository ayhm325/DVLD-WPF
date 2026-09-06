using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LicenseRepository(DVLDDbContext context)
    : ILicenseRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<License> Query() =>
        _context.Licenses
            .Include(l => l.Driver)
                .ThenInclude(d => d.Person)
            .Include(l => l.Driver)
                .ThenInclude(d => d.CreatedByUser)
            .Include(l => l.Driver)
                .ThenInclude(d => d.Licenses
                    .Where(x => x.IsActive))
            .Include(l => l.LicenseClassInfo)
            .Include(l => l.CreatedByUser);

    public Task<License?> GetLicenseByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<License?>(null)
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    l => l.LicenseID == id);

    public Task<License?> GetByDriverIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult<License?>(null)
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .FirstOrDefaultAsync();

    public Task<List<License>> GetAllLicensesAsync() =>
        Query()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();

    public Task<List<License>> GetLicensesByDriverIdAsync(
        int driverId) =>
        driverId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByApplicationIdAsync(
        int applicationId) =>
        applicationId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.ApplicationID == applicationId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByLicenseClassIdAsync(
        int licenseClassId) =>
        licenseClassId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.LicenseClass == licenseClassId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByPersonIdAsync(
        int personId) =>
        personId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.Driver.PersonID == personId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<bool> IsLicenseExistsAsync(int id) =>
        id <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.LicenseID == id);

    public Task<bool> IsDriverHasLicenseAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.DriverID == driverId);

    public Task<bool> IsApplicationHasLicenseAsync(
        int applicationId) =>
        applicationId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(
                    l => l.ApplicationID == applicationId);

    public Task<bool> IsActiveLicenseExistsAsync(
        int driverId,
        int licenseClassId) =>
        driverId <= 0 || licenseClassId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l =>
                    l.DriverID == driverId &&
                    l.LicenseClass == licenseClassId &&
                    l.IsActive);

    public async Task<HashSet<int>>
        GetApplicationIdsWithLicensesAsync(
            IEnumerable<int> applicationIds)
    {
        ArgumentNullException.ThrowIfNull(applicationIds);

        var ids = applicationIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var result = await _context.Licenses
            .AsNoTracking()
            .Where(l => ids.Contains(l.ApplicationID))
            .Select(l => l.ApplicationID)
            .Distinct()
            .ToListAsync();

        return result.ToHashSet();
    }

    public Task AddLicenseAsync(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        return _context.Licenses
            .AddAsync(license)
            .AsTask();
    }

    public async Task<bool> DeactivateLicenseAsync(
        int licenseId)
    {
        if (licenseId <= 0)
            return false;

        var affectedRows =
            await _context.Licenses
                .Where(l =>
                    l.LicenseID == licenseId &&
                    l.IsActive)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        l => l.IsActive,
                        false));

        return affectedRows > 0;
    }

    public Task<bool> HasAnotherActiveLicenseAsync(
        int driverId,
        int licenseClassId,
        int excludedLicenseId) =>
        driverId <= 0 ||
        licenseClassId <= 0 ||
        excludedLicenseId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l =>
                    l.DriverID == driverId &&
                    l.LicenseClass == licenseClassId &&
                    l.LicenseID != excludedLicenseId &&
                    l.IsActive);

    public async Task<bool> ActivateLicenseAsync(
        int licenseId)
    {
        if (licenseId <= 0)
            return false;

        var affectedRows =
            await _context.Licenses
                .Where(l =>
                    l.LicenseID == licenseId &&
                    !l.IsActive &&
                    l.ExpirationDate > DateTime.UtcNow)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        l => l.IsActive,
                        true));

        return affectedRows > 0;
    }
}
