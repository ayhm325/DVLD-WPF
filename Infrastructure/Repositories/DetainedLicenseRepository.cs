using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DetainedLicenseRepository(
    DVLDDbContext context) : IDetainedLicenseRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<DetainedLicense> Query() =>
        _context.DetainedLicenses
            .Include(d => d.License)
                .ThenInclude(l => l.Driver)
                    .ThenInclude(d => d.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ReleasedByUser)
            .Include(d => d.ReleaseApplication);

    public Task<List<DetainedLicense>> GetAllAsync() =>
        Query()
            .AsNoTracking()
            .OrderByDescending(d => d.DetainDate)
            .ToListAsync();

    public Task<DetainedLicense?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<DetainedLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DetainID == id);

    public Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(
        int licenseId) =>
        licenseId <= 0
            ? Task.FromResult<DetainedLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);

    public Task<bool> IsLicenseDetainedAsync(int licenseId) =>
        licenseId > 0
            ? _context.DetainedLicenses
                .AsNoTracking()
                .AnyAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased)
            : Task.FromResult(false);

    public async Task AddAsync(DetainedLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.DetainedLicenses.AddAsync(entity);
    }

    public Task<DetainedLicense?> GetByIdForUpdateAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult<DetainedLicense?>(null);

        return _context.DetainedLicenses
            .FromSqlInterpolated($"""
                SELECT *
                FROM DetainedLicenses WITH (UPDLOCK, HOLDLOCK)
                WHERE DetainID = {id}
                """)
            .Include(d => d.License)
                .ThenInclude(l => l.Driver)
                    .ThenInclude(d => d.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.ReleasedByUser)
            .Include(d => d.ReleaseApplication)
            .FirstOrDefaultAsync();
    }
}
