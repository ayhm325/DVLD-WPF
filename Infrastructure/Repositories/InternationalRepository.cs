using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InternationalRepository : IInternationalRepository
{
    private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

    public InternationalRepository(
        IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(nameof(contextFactory));
    }


    // =========================================================
    // BASE QUERY
    // =========================================================

    private static IQueryable<InternationalLicense> Query(
        DVLDDbContext context)
    {
        return context.InternationalLicenses

            // Application
            .Include(i => i.Application)

            // Driver -> Person
            .Include(i => i.Driver)
                .ThenInclude(d => d.Person)

            // Local License
            .Include(i => i.IssuedUsingLocalLicense)

            // Created By User
            .Include(i => i.CreatedByUser);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<InternationalLicense>>
        GetAllAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .AsNoTracking()
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.InternationalLicenseID ==
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .AsNoTracking()
            .Where(i => i.DriverID == driverId)
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.ApplicationID ==
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .AsNoTracking()
            .Where(i =>
                i.IssuedUsingLocalLicenseID ==
                localLicenseId)
            .ToListAsync();
    }


    // =========================================================
    // CHECK - EXISTS BY LOCAL LICENSE
    // =========================================================

    public async Task<bool>
        ExistsByLocalLicenseAsync(
            int localLicenseId)
    {
        if (localLicenseId <= 0)
            return false;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.InternationalLicenses
            .AsNoTracking()
            .AnyAsync(i =>
                i.IssuedUsingLocalLicenseID ==
                localLicenseId);
    }


    // =========================================================
    // CHECK - ACTIVE INTERNATIONAL LICENSE
    // =========================================================

    public async Task<bool>
        HasActiveInternationalLicenseAsync(
            int driverId)
    {
        if (driverId <= 0)
            return false;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.InternationalLicenses
            .AsNoTracking()
            .AnyAsync(i =>
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
        ArgumentNullException.ThrowIfNull(entity);

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        await context.InternationalLicenses
            .AddAsync(entity);

        await context.SaveChangesAsync();

        return entity.InternationalLicenseID;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateAsync(
            InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.InternationalLicenseID <= 0)
            return false;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var existing =
            await context.InternationalLicenses
                .FirstOrDefaultAsync(i =>
                    i.InternationalLicenseID ==
                    entity.InternationalLicenseID);

        if (existing is null)
            return false;

        context.Entry(existing)
            .CurrentValues
            .SetValues(entity);

        return await context.SaveChangesAsync() > 0;
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var entity =
            await context.InternationalLicenses
                .FirstOrDefaultAsync(i =>
                    i.InternationalLicenseID ==
                    internationalLicenseId);

        if (entity is null)
            return false;

        context.InternationalLicenses
            .Remove(entity);

        return await context.SaveChangesAsync() > 0;
    }
}