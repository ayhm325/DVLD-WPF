using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DriverRepository(DVLDDbContext context) : IDriverRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<Driver> Query() =>
        _context.Drivers
            .Include(d => d.Person)
            .Include(d => d.CreatedByUser)
            .Include(d => d.Licenses)
            .Include(d => d.InternationalLicenses);

    public Task<Driver?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : Query().FirstOrDefaultAsync(d => d.DriverID == id);

    public Task<List<Driver>> GetAllAsync() =>
        Query()
            .AsNoTracking()
            .OrderBy(d => d.DriverID)
            .ToListAsync();

    public Task<Driver?> GetByPersonIdAsync(int personId) =>
        personId <= 0
            ? Task.FromResult<Driver?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.PersonID == personId);

    public Task<List<Driver>> GetByCreatedUserIdAsync(int userId) =>
        userId <= 0
            ? Task.FromResult<List<Driver>>([])
            : Query()
                .AsNoTracking()
                .Where(d => d.CreatedByUserID == userId)
                .ToListAsync();

    public Task<bool> ExistsByIdAsync(int driverId) =>
        driverId > 0
            ? _context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.DriverID == driverId)
            : Task.FromResult(false);

    public Task<bool> ExistsByPersonIdAsync(int personId) =>
        personId > 0
            ? _context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.PersonID == personId)
            : Task.FromResult(false);

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