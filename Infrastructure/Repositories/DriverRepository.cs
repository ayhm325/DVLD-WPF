using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public DriverRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================

        private IQueryable<Driver> Query(DVLDDbContext context)
        {
            return context.Drivers
                .Include(d => d.Person)
                .Include(d => d.CreatedByUser)
                .Include(d => d.Licenses);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<Driver?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(d => d.DriverID == id);
        }

        public async Task<List<Driver>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<Driver?> GetByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(d => d.PersonID == personId);
        }

        public async Task<List<Driver>> GetByCreatedUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(d => d.CreatedByUserID == userId)
                .ToListAsync();
        }



        // =========================
        // CHECK OPERATIONS
        // =========================

        public async Task<bool> ExistsAsync(Expression<Func<Driver, bool>> predicate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Drivers
                .AsNoTracking()
                .AnyAsync(predicate);
        }

        public async Task<bool> ExistsByIdAsync(int driverId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.DriverID == driverId);
        }

        public async Task<bool> ExistsByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Drivers
                .AsNoTracking()
                .AnyAsync(d => d.PersonID == personId);
        }

        // =========================
        // COMMAND OPERATIONS
        // =========================

        public async Task AddAsync(Driver driver)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.Drivers.AddAsync(driver);

            await context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Driver driver)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Drivers
                .FirstOrDefaultAsync(d => d.DriverID == driver.DriverID);

            if (existing is null)
                throw new InvalidOperationException("Driver not found.");

            context.Entry(existing).CurrentValues.SetValues(driver);

            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var driver = await context.Drivers.FindAsync(id);

            if (driver is null)
                return;

            context.Drivers.Remove(driver);

            await context.SaveChangesAsync();
        }
    }
}