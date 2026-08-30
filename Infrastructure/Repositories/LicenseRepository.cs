using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LicenseRepository
    : ILicenseRepository
{
    private readonly DVLDDbContext _context;

    public LicenseRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    private IQueryable<License> Query()
    {
        return _context.Licenses
            .Include(l => l.Application)
            .Include(l => l.Driver)
                .ThenInclude(d => d.Person)
            .Include(l => l.LicenseClassInfo)
            .Include(l => l.CreatedByUser);
    }

    public async Task<License?>
        GetLicenseByIdAsync(int id)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.LicenseID == id);
    }

    public async Task<List<License>>
        GetAllLicensesAsync()
    {
        return await Query()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<License?>
        GetByDriverIdAsync(int driverId)
    {
        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.DriverID == driverId);
    }

    public async Task<List<License>>
        GetLicensesByDriverIdAsync(
            int driverId)
    {
        return await Query()
            .AsNoTracking()
            .Where(l =>
                l.DriverID == driverId)
            .OrderByDescending(
                l => l.IssueDate)
            .ToListAsync();
    }

    public async Task<List<License>>
        GetLicensesByApplicationIdAsync(
            int applicationId)
    {
        return await Query()
            .AsNoTracking()
            .Where(l =>
                l.ApplicationID == applicationId)
            .ToListAsync();
    }

    public async Task<List<License>>
        GetLicensesByLicenseClassIdAsync(
            int licenseClassId)
    {
        return await Query()
            .AsNoTracking()
            .Where(l =>
                l.LicenseClass == licenseClassId)
            .ToListAsync();
    }

    public async Task<List<License>>
        GetLicensesByPersonIdAsync(
            int personId)
    {
        return await Query()
            .AsNoTracking()
            .Where(l =>
                l.Driver.PersonID == personId)
            .ToListAsync();
    }

    public async Task<bool>
        IsLicenseExistsAsync(int id)
    {
        return await _context.Licenses
            .AnyAsync(
                l => l.LicenseID == id);
    }

    public async Task<bool>
        IsDriverHasLicenseAsync(int driverId)
    {
        return await _context.Licenses
            .AnyAsync(
                l => l.DriverID == driverId);
    }

    public async Task<bool>
        IsApplicationHasLicenseAsync(
            int applicationId)
    {
        return await _context.Licenses
            .AnyAsync(
                l => l.ApplicationID == applicationId);
    }

    public async Task<int>
        AddLicenseAsync(
            License license)
    {
        ArgumentNullException.ThrowIfNull(
            license);

        await _context.Licenses
            .AddAsync(license);

        return license.LicenseID;
    }

    public async Task<bool>
        UpdateLicenseAsync(
            License license)
    {
        ArgumentNullException.ThrowIfNull(
            license);

        if (license.LicenseID <= 0)
            return false;

        var existing =
            await _context.Licenses
                .FirstOrDefaultAsync(
                    l =>
                        l.LicenseID ==
                        license.LicenseID);

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(license);

        return true;
    }

    public async Task<bool>
        DeleteLicenseAsync(int id)
    {
        if (id <= 0)
            return false;

        var license =
            await _context.Licenses
                .FirstOrDefaultAsync(
                    l =>
                        l.LicenseID == id);

        if (license is null)
            return false;

        _context.Licenses.Remove(
            license);

        return true;
    }
}
