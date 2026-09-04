using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly DVLDDbContext _context;

    public DriverRepository(DVLDDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ============ Base Query ============

    private IQueryable<Driver> Query()
    {
        return _context.Drivers
            .Include(d => d.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Licenses)
            .Include(d => d.InternationalLicenses);
    }

    // ============ Queries ============

    // Tracking is intentional for DriverService.UpdateAsync() modifications
    public async Task<Driver?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;

        return await Query()
            .FirstOrDefaultAsync(d => d.DriverID == id);
    }

    public async Task<List<Driver>> GetAllAsync()
    {
        return await Query()
            .AsNoTracking()
            .OrderBy(d => d.DriverID)
            .ToListAsync();
    }

    public async Task<Driver?> GetByPersonIdAsync(int personId)
    {
        if (personId <= 0) return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PersonID == personId);
    }

    public async Task<List<Driver>> GetByCreatedUserIdAsync(int userId)
    {
        if (userId <= 0) return [];

        return await Query()
            .AsNoTracking()
            .Where(d => d.CreatedByUserID == userId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<Driver, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await _context.Drivers.AsNoTracking().AnyAsync(predicate);
    }

    public async Task<bool> ExistsByIdAsync(int driverId)
    {
        if (driverId <= 0) return false;
        return await _context.Drivers.AsNoTracking().AnyAsync(d => d.DriverID == driverId);
    }

    public async Task<bool> ExistsByPersonIdAsync(int personId)
    {
        if (personId <= 0) return false;
        return await _context.Drivers.AsNoTracking().AnyAsync(d => d.PersonID == personId);
    }

    // ============ Mutations ============

    // Repository only stages the entity; UnitOfWork owns persistence
    public async Task AddAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        await _context.Drivers.AddAsync(driver);
    }

    public async Task UpdateAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        if (driver.DriverID <= 0)
            throw new ArgumentException("Driver ID must be greater than zero.", nameof(driver));

        var existing = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverID == driver.DriverID);
        if (existing is null)
            throw new InvalidOperationException("Driver not found.");

        _context.Entry(existing).CurrentValues.SetValues(driver);
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0) return;

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverID == id);
        if (driver is null) return;

        _context.Drivers.Remove(driver);
    }
}