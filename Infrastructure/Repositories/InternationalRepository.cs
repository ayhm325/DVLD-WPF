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

    public Task<List<InternationalLicense>> GetAllAsync() =>
        Query()
            .AsNoTracking()
            .OrderByDescending(i => i.InternationalLicenseID)
            .ToListAsync();

    public Task<InternationalLicense?> GetByIdAsync(
        int internationalLicenseId) =>
        internationalLicenseId <= 0
            ? Task.FromResult<InternationalLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.InternationalLicenseID == internationalLicenseId);

    public Task<List<InternationalLicense>> GetByDriverIdAsync(
        int driverId) =>
        driverId <= 0
            ? Task.FromResult<List<InternationalLicense>>([])
            : Query()
                .AsNoTracking()
                .Where(i => i.DriverID == driverId)
                .OrderByDescending(i => i.InternationalLicenseID)
                .ToListAsync();

    public Task<InternationalLicense?> GetByApplicationIdAsync(
        int applicationId) =>
        applicationId <= 0
            ? Task.FromResult<InternationalLicense?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.ApplicationID == applicationId);

    public Task<List<InternationalLicense>> GetByLocalLicenseIdAsync(
        int localLicenseId) =>
        localLicenseId <= 0
            ? Task.FromResult<List<InternationalLicense>>([])
            : Query()
                .AsNoTracking()
                .Where(
                    i => i.IssuedUsingLocalLicenseID == localLicenseId)
                .OrderByDescending(i => i.InternationalLicenseID)
                .ToListAsync();

    public Task<bool> ExistsByLocalLicenseAsync(
        int localLicenseId) =>
        localLicenseId > 0
            ? _context.InternationalLicenses
                .AsNoTracking()
                .AnyAsync(
                    i => i.IssuedUsingLocalLicenseID == localLicenseId)
            : Task.FromResult(false);

    public Task<bool> HasActiveInternationalLicenseAsync(
        int driverId) =>
        driverId > 0
            ? _context.InternationalLicenses
                .AsNoTracking()
                .AnyAsync(
                    i =>
                        i.DriverID == driverId &&
                        i.IsActive &&
                        i.ExpirationDate > DateTime.UtcNow)
            : Task.FromResult(false);

    public async Task AddAsync(
        InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.InternationalLicenses.AddAsync(entity);
    }
}
