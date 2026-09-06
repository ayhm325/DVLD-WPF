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
        .Include(l => l.Application)
        .Include(l => l.Driver)
            .ThenInclude(d => d.Person)
        .Include(l => l.Driver)
            .ThenInclude(d => d.CreatedByUser)
        .Include(l => l.LicenseClassInfo)
        .Include(l => l.CreatedByUser);

    public Task<License?> GetLicenseByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<License?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LicenseID == id);

    public Task<License?> GetByDriverIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult<License?>(null)
            : Query()
                .AsNoTracking()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .FirstOrDefaultAsync();

    public Task<List<License>> GetAllLicensesAsync() =>
        Query()
            .AsNoTracking()
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();

    public Task<List<License>> GetLicensesByDriverIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByApplicationIdAsync(
        int applicationId) =>
        applicationId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .Where(l => l.ApplicationID == applicationId)
                .ToListAsync();

    public Task<List<License>> GetLicensesByLicenseClassIdAsync(
        int licenseClassId) =>
        licenseClassId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .Where(l => l.LicenseClass == licenseClassId)
                .ToListAsync();

    public Task<List<License>> GetLicensesByPersonIdAsync(int personId) =>
        personId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .Where(l => l.Driver.PersonID == personId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<bool> IsLicenseExistsAsync(int id) =>
    id > 0
        ? _context.Licenses
            .AsNoTracking()
            .AnyAsync(l => l.LicenseID == id)
        : Task.FromResult(false);

    public Task<bool> IsDriverHasLicenseAsync(int driverId) =>
        driverId > 0
            ? _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.DriverID == driverId)
            : Task.FromResult(false);

    public Task<bool> IsApplicationHasLicenseAsync(int applicationId) =>
        applicationId > 0
            ? _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.ApplicationID == applicationId)
            : Task.FromResult(false);

    public Task<bool> IsActiveLicenseExistsAsync(
        int driverId,
        int licenseClassId) =>
        driverId > 0 && licenseClassId > 0
            ? _context.Licenses
                .AsNoTracking()
                .AnyAsync(l =>
                    l.DriverID == driverId &&
                    l.LicenseClass == licenseClassId &&
                    l.IsActive)
            : Task.FromResult(false);

    public async Task<HashSet<int>> GetApplicationIdsWithLicensesAsync(
        IEnumerable<int> applicationIds)
    {
        ArgumentNullException.ThrowIfNull(applicationIds);

        var ids = applicationIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        return (
            await _context.Licenses
                .AsNoTracking()
                .Where(l => ids.Contains(l.ApplicationID))
                .Select(l => l.ApplicationID)
                .Distinct()
                .ToListAsync()
        ).ToHashSet();
    }

    public Task AddLicenseAsync(License license)
    {
        ArgumentNullException.ThrowIfNull(license);
        return _context.Licenses.AddAsync(license).AsTask();
    }

    public async Task<bool> UpdateLicenseAsync(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        if (license.LicenseID <= 0)
            return false;

        var existing = await _context.Licenses
            .FirstOrDefaultAsync(l => l.LicenseID == license.LicenseID);

        if (existing is null)
            return false;

        _context.Entry(existing).CurrentValues.SetValues(license);
        return true;
    }

    public async Task<bool> DeleteLicenseAsync(int id)
    {
        if (id <= 0)
            return false;

        var license = await _context.Licenses
            .FirstOrDefaultAsync(l => l.LicenseID == id);

        if (license is null)
            return false;

        _context.Licenses.Remove(license);
        return true;
    }

    public Task<bool> HasAnotherActiveLicenseAsync(
    int driverId,
    int licenseClassId,
    int excludedLicenseId) =>
    driverId > 0 &&
    licenseClassId > 0 &&
    excludedLicenseId > 0
        ? _context.Licenses
            .AsNoTracking()
            .AnyAsync(l =>
                l.DriverID == driverId &&
                l.LicenseClass == licenseClassId &&
                l.LicenseID != excludedLicenseId &&
                l.IsActive)
        : Task.FromResult(false);
}