using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DriverRepository(DVLDDbContext context)
    : IDriverRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<Driver> QueryWithBasicInfo() =>
        _context.Drivers
            .Include(d => d.Person)
            .Include(d => d.CreatedByUser);

    private IQueryable<Driver> QueryForDelete() =>
        _context.Drivers
            .Include(d => d.Licenses)
            .Include(d => d.InternationalLicenses);

    public Task<Driver?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryWithBasicInfo()
                .FirstOrDefaultAsync(d => d.DriverID == id);

    public Task<Driver?> GetForDeleteAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryForDelete()
                .FirstOrDefaultAsync(d => d.DriverID == id);

    public Task<List<Driver>> GetAllAsync() =>
        QueryWithBasicInfo()
            .AsNoTracking()
            .OrderBy(d => d.DriverID)
            .ToListAsync();

    public Task<Driver?> GetByPersonIdAsync(int personId) =>
        personId <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryWithBasicInfo()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.PersonID == personId);

    public Task<List<Driver>> GetByCreatedUserIdAsync(int userId) =>
        userId <= 0
            ? Task.FromResult<List<Driver>>([])
            : QueryWithBasicInfo()
                .AsNoTracking()
                .Where(d => d.CreatedByUserID == userId)
                .OrderBy(d => d.DriverID)
                .ToListAsync();

    public async Task<bool> ExistsByIdAsync(int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _context.Drivers
            .AsNoTracking()
            .AnyAsync(d => d.DriverID == driverId);
    }

    public async Task<bool> ExistsByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return false;

        return await _context.Drivers
            .AsNoTracking()
            .AnyAsync(d => d.PersonID == personId);
    }

    public async Task AddAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        await _context.Drivers.AddAsync(driver);
    }

    public void Delete(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        _context.Drivers.Remove(driver);
    }
}