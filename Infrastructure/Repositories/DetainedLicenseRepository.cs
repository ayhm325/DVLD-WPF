using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DetainedLicenseRepository
    : IDetainedLicenseRepository
{
    private readonly DVLDDbContext _context;

    public DetainedLicenseRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<DetainedLicense?>
        GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await _context.DetainedLicenses
            .AsNoTracking()
            .Include(d => d.License)
                .ThenInclude(l => l.Driver)
                    .ThenInclude(dr => dr.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ReleasedByUser)
            .Include(d => d.ReleaseApplication)
            .FirstOrDefaultAsync(
                d => d.DetainID == id);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<DetainedLicense>>
        GetAllAsync()
    {
        return await _context.DetainedLicenses
            .AsNoTracking()
            .Include(d => d.License)
                .ThenInclude(l => l.Driver)
                    .ThenInclude(dr => dr.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ReleasedByUser)
            .Include(d => d.ReleaseApplication)
            .OrderByDescending(
                d => d.DetainDate)
            .ToListAsync();
    }

    // =========================================================
    // CHECK ACTIVE DETENTION
    // =========================================================

    public async Task<bool>
        IsLicenseDetainedAsync(
            int licenseId)
    {
        if (licenseId <= 0)
            return false;

        return await _context.DetainedLicenses
            .AsNoTracking()
            .AnyAsync(
                d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);
    }

    // =========================================================
    // GET ACTIVE DETENTION
    // =========================================================

    public async Task<DetainedLicense?>
        GetActiveDetainByLicenseIdAsync(
            int licenseId)
    {
        if (licenseId <= 0)
            return null;

        return await _context.DetainedLicenses
            .AsNoTracking()
            .Include(d => d.License)
                .ThenInclude(l => l.Driver)
                    .ThenInclude(dr => dr.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ReleasedByUser)
            .Include(d => d.ReleaseApplication)
            .FirstOrDefaultAsync(
                d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<DetainedLicense>
        AddAsync(
            DetainedLicense entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        await _context.DetainedLicenses
            .AddAsync(entity);

        // IMPORTANT:
        // No SaveChangesAsync here.
        //
        // UnitOfWork owns persistence.

        return entity;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task
        UpdateAsync(
            DetainedLicense entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        if (entity.DetainID <= 0)
        {
            throw new ArgumentException(
                "Detain ID must be greater than zero.",
                nameof(entity));
        }

        var existing =
            await _context.DetainedLicenses
                .FirstOrDefaultAsync(
                    d =>
                        d.DetainID ==
                        entity.DetainID);

        if (existing is null)
        {
            throw new InvalidOperationException(
                $"Detained license with ID " +
                $"{entity.DetainID} was not found.");
        }

        existing.FineFees =
            entity.FineFees;

        existing.IsReleased =
            entity.IsReleased;

        existing.ReleaseDate =
            entity.ReleaseDate;

        existing.ReleasedByUserID =
            entity.ReleasedByUserID;

        existing.ReleaseApplicationID =
            entity.ReleaseApplicationID;

        // IMPORTANT:
        // No SaveChangesAsync here.
    }
}