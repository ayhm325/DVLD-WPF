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

    public Task<List<DetainedLicense>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Query()
            .AsNoTracking()
            .OrderByDescending(d => d.DetainDate)
            .ToListAsync(cancellationToken);

    public Task<DetainedLicense?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<DetainedLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DetainID == id, cancellationToken);

    public Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(
        int licenseId,
        CancellationToken cancellationToken = default) =>
        licenseId <= 0
            ? Task.FromResult<DetainedLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased,
                    cancellationToken);

    public Task<bool> IsLicenseDetainedAsync(int licenseId, CancellationToken cancellationToken = default) =>
        licenseId > 0
            ? _context.DetainedLicenses
                .AsNoTracking()
                .AnyAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased,
                    cancellationToken)
            : Task.FromResult(false);

    public async Task<HashSet<int>> GetActiveDetainedLicenseIdsAsync(
        IEnumerable<int> licenseIds,
        CancellationToken cancellationToken = default)
    {
        if (licenseIds is null)
            return [];

        var ids = licenseIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var activeIds = await _context.DetainedLicenses
            .AsNoTracking()
            .Where(d => !d.IsReleased && ids.Contains(d.LicenseID))
            .Select(d => d.LicenseID)
            .ToListAsync(cancellationToken);

        return activeIds.ToHashSet();
    }

    public async Task AddAsync(DetainedLicense entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.DetainedLicenses.AddAsync(entity, cancellationToken);
    }

    public Task<DetainedLicense?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
