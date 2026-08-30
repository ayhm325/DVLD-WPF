using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InternationalRepository
    : IInternationalRepository
{
    private readonly DVLDDbContext _context;

    public InternationalRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // BASE QUERY
    // =========================================================

    private IQueryable<InternationalLicense>
        Query()
    {
        return _context.InternationalLicenses
            .Include(i => i.Application)
            .Include(i => i.Driver)
                .ThenInclude(d => d.Person)
            .Include(i => i.IssuedUsingLocalLicense);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<InternationalLicense>>
        GetAllAsync()
    {
        return await Query()
            .AsNoTracking()
            .OrderByDescending(
                i => i.InternationalLicenseID)
            .ToListAsync();
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<InternationalLicense?>
        GetByIdAsync(
            int internationalLicenseId)
    {
        if (internationalLicenseId <= 0)
            return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i =>
                    i.InternationalLicenseID ==
                    internationalLicenseId);
    }

    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    public async Task<List<InternationalLicense>>
        GetByDriverIdAsync(
            int driverId)
    {
        if (driverId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(
                i =>
                    i.DriverID == driverId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<InternationalLicense?>
        GetByApplicationIdAsync(
            int applicationId)
    {
        if (applicationId <= 0)
            return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i =>
                    i.ApplicationID ==
                    applicationId);
    }

    // =========================================================
    // GET BY LOCAL LICENSE ID
    // =========================================================

    public async Task<List<InternationalLicense>>
        GetByLocalLicenseIdAsync(
            int localLicenseId)
    {
        if (localLicenseId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(
                i =>
                    i.IssuedUsingLocalLicenseID ==
                    localLicenseId)
            .ToListAsync();
    }

    // =========================================================
    // CHECK
    // =========================================================

    public async Task<bool>
        ExistsByLocalLicenseAsync(
            int localLicenseId)
    {
        if (localLicenseId <= 0)
            return false;

        return await _context
            .InternationalLicenses
            .AsNoTracking()
            .AnyAsync(
                i =>
                    i.IssuedUsingLocalLicenseID ==
                    localLicenseId);
    }

    // =========================================================
    // CHECK ACTIVE
    // =========================================================

    public async Task<bool>
        HasActiveInternationalLicenseAsync(
            int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _context
            .InternationalLicenses
            .AsNoTracking()
            .AnyAsync(
                i =>
                    i.DriverID == driverId &&
                    i.IsActive);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<int>
        AddAsync(
            InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        await _context
            .InternationalLicenses
            .AddAsync(entity);

        // No SaveChangesAsync.
        // UnitOfWork owns persistence.

        return entity.InternationalLicenseID;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateAsync(
            InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        if (entity.InternationalLicenseID <= 0)
            return false;

        var existing =
            await _context
                .InternationalLicenses
                .FirstOrDefaultAsync(
                    i =>
                        i.InternationalLicenseID ==
                        entity.InternationalLicenseID);

        if (existing is null)
            return false;

        existing.ApplicationID =
            entity.ApplicationID;

        existing.DriverID =
            entity.DriverID;

        existing.IssuedUsingLocalLicenseID =
            entity.IssuedUsingLocalLicenseID;

        existing.IssueDate =
            entity.IssueDate;

        existing.ExpirationDate =
            entity.ExpirationDate;

        existing.IsActive =
            entity.IsActive;

        existing.CreatedByUserID =
            entity.CreatedByUserID;

        // No SaveChangesAsync.

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteAsync(
            int internationalLicenseId)
    {
        if (internationalLicenseId <= 0)
            return false;

        var entity =
            await _context
                .InternationalLicenses
                .FirstOrDefaultAsync(
                    i =>
                        i.InternationalLicenseID ==
                        internationalLicenseId);

        if (entity is null)
            return false;

        _context
            .InternationalLicenses
            .Remove(entity);

        // No SaveChangesAsync.

        return true;
    }
}