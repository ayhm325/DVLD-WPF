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

    public async Task<Driver?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var result = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.DriverID == id)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .FirstOrDefaultAsync();

        if (result is null)
            return null;

        result.Driver.ActiveLicensesCount = result.ActiveCount;
        return result.Driver;
    }

    public Task<Driver?> GetForUpdateAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : _context.Drivers
                .FirstOrDefaultAsync(d => d.DriverID == id);

    public Task<Driver?> GetForDeleteAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryForDelete()
                .FirstOrDefaultAsync(
                    d => d.DriverID == id);

    public async Task<List<Driver>> GetAllAsync()
    {
        var results = await QueryWithBasicInfo()
            .AsNoTracking()
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .OrderBy(x => x.Driver.DriverID)
            .ToListAsync();

        foreach (var item in results)
        {
            item.Driver.ActiveLicensesCount = item.ActiveCount;
        }

        return results.Select(x => x.Driver).ToList();
    }

    public async Task<Driver?> GetByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return null;

        var result = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.PersonID == personId)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .FirstOrDefaultAsync();

        if (result is null)
            return null;

        result.Driver.ActiveLicensesCount = result.ActiveCount;
        return result.Driver;
    }

    public async Task<List<Driver>> GetByCreatedUserIdAsync(int userId)
    {
        if (userId <= 0)
            return [];

        var results = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.CreatedByUserID == userId)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .OrderBy(x => x.Driver.DriverID)
            .ToListAsync();

        foreach (var item in results)
        {
            item.Driver.ActiveLicensesCount = item.ActiveCount;
        }

        return results.Select(x => x.Driver).ToList();
    }

    public Task<bool> ExistsByIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.DriverID == driverId);

    public Task<bool> ExistsByPersonIdAsync(
        int personId) =>
        personId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.PersonID == personId);

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
