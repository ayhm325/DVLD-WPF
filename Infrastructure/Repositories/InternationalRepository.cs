using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class InternationalRepository(
    DVLDDbContext context) : IInternationalRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<InternationalLicense> Query() =>
        _context.InternationalLicenses
            .Include(i => i.Application)
            .Include(i => i.Driver)
                .ThenInclude(d => d.Person)
            .Include(i => i.IssuedUsingLocalLicense)
            .Include(i => i.CreatedByUser);

    public Task<List<InternationalLicense>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Query()
            .AsNoTracking()
            .OrderByDescending(i => i.InternationalLicenseID)
            .ToListAsync(cancellationToken);

    public Task<InternationalLicense?> GetByIdAsync(
        int internationalLicenseId,
        CancellationToken cancellationToken = default) =>
        internationalLicenseId <= 0
            ? Task.FromResult<InternationalLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.InternationalLicenseID == internationalLicenseId,
                    cancellationToken);

    public Task<List<InternationalLicense>> GetByDriverIdAsync(
        int driverId,
        CancellationToken cancellationToken = default) =>
        driverId <= 0
            ? Task.FromResult<List<InternationalLicense>>([])
            : Query()
                .AsNoTracking()
                .Where(i => i.DriverID == driverId)
                .OrderByDescending(i => i.InternationalLicenseID)
                .ToListAsync(cancellationToken);

    public Task<InternationalLicense?> GetByApplicationIdAsync(
        int applicationId,
        CancellationToken cancellationToken = default) =>
        applicationId <= 0
            ? Task.FromResult<InternationalLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.ApplicationID == applicationId,
                    cancellationToken);

    public Task<List<InternationalLicense>> GetByLocalLicenseIdAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default) =>
        localLicenseId <= 0
            ? Task.FromResult<List<InternationalLicense>>([])
            : Query()
                .AsNoTracking()
                .Where(
                    i => i.IssuedUsingLocalLicenseID == localLicenseId)
                .OrderByDescending(i => i.InternationalLicenseID)
                .ToListAsync(cancellationToken);

    public Task<bool> ExistsByLocalLicenseAsync(
        int localLicenseId,
        CancellationToken cancellationToken = default) =>
        localLicenseId > 0
            ? _context.InternationalLicenses
                .AsNoTracking()
                .AnyAsync(
                    i => i.IssuedUsingLocalLicenseID == localLicenseId,
                    cancellationToken)
            : Task.FromResult(false);

    public Task<bool> HasActiveInternationalLicenseAsync(
        int driverId,
        DateOnly jordanToday,
        CancellationToken cancellationToken = default)
    {
        if (driverId <= 0)
            return Task.FromResult(false);

        var jordanTodayStart = jordanToday.ToDateTime(TimeOnly.MinValue);

        return _context.InternationalLicenses
            .AsNoTracking()
            .AnyAsync(
                i =>
                    i.DriverID == driverId &&
                    i.IsActive &&
                    i.ExpirationDate >= jordanTodayStart,
                cancellationToken);
    }

    public async Task AddAsync(
        InternationalLicense entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.InternationalLicenses.AddAsync(entity, cancellationToken);
    }
}
