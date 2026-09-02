using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LicenseRepository : ILicenseRepository
{
    private readonly DVLDDbContext _context;

    public LicenseRepository(DVLDDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // =========================================================
    // BASE QUERY
    // =========================================================

    private IQueryable<License> Query()
    {
        return _context.Licenses
            .Include(l => l.Application)
            .Include(l => l.Driver)
                .ThenInclude(d => d.Person)
            .Include(l => l.LicenseClassInfo)
            .Include(l => l.CreatedByUser);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<License?> GetLicenseByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LicenseID == id);
    }

    // =========================================================
    // GET BY DRIVER
    // =========================================================

    public async Task<License?> GetByDriverIdAsync(int driverId)
    {
        if (driverId <= 0)
            return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.DriverID == driverId);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<License>> GetAllLicensesAsync()
    {
        return await Query()
            .AsNoTracking()
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();
    }

    // =========================================================
    // GET BY DRIVER (LIST)
    // =========================================================

    public async Task<List<License>> GetLicensesByDriverIdAsync(int driverId)
    {
        if (driverId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(l => l.DriverID == driverId)
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();
    }

    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    public async Task<List<License>> GetLicensesByApplicationIdAsync(int applicationId)
    {
        if (applicationId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(l => l.ApplicationID == applicationId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY LICENSE CLASS
    // =========================================================

    public async Task<List<License>> GetLicensesByLicenseClassIdAsync(int licenseClassId)
    {
        if (licenseClassId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(l => l.LicenseClass == licenseClassId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY PERSON
    // =========================================================

    public async Task<List<License>> GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(l => l.Driver.PersonID == personId)
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();
    }

    // =========================================================
    // EXISTS BY LICENSE ID
    // =========================================================

    public async Task<bool> IsLicenseExistsAsync(int id)
    {
        if (id <= 0)
            return false;

        return await _context.Licenses
            .AsNoTracking()
            .AnyAsync(l => l.LicenseID == id);
    }

    // =========================================================
    // DRIVER HAS LICENSE
    // =========================================================

    public async Task<bool> IsDriverHasLicenseAsync(int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _context.Licenses
            .AsNoTracking()
            .AnyAsync(l => l.DriverID == driverId);
    }

    // =========================================================
    // APPLICATION HAS LICENSE
    // =========================================================

    public async Task<bool> IsApplicationHasLicenseAsync(int applicationId)
    {
        if (applicationId <= 0)
            return false;

        return await _context.Licenses
            .AsNoTracking()
            .AnyAsync(l => l.ApplicationID == applicationId);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task AddLicenseAsync(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        await _context.Licenses.AddAsync(license);       
    }

    // =========================================================
    // UPDATE
    // =========================================================

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

        // DO NOT SAVE HERE — IUnitOfWork owns persistence.
        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool> DeleteLicenseAsync(int id)
    {
        if (id <= 0)
            return false;

        var license = await _context.Licenses
            .FirstOrDefaultAsync(l => l.LicenseID == id);

        if (license is null)
            return false;

        _context.Licenses.Remove(license);

        // DO NOT SAVE HERE — IUnitOfWork owns persistence.
        return true;
    }
}