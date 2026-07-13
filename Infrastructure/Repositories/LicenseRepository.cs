using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LicenseRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public LicenseRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================
        private IQueryable<License> Query(DVLDDbContext context)
        {
            return context.Licenses
                .AsNoTracking()
                .Include(l => l.Application)
                .Include(l => l.Driver)
                .Include(l => l.LicenseClassInfo)
                .Include(l => l.CreatedByUser);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<License?> GetLicenseByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(l => l.LicenseID == id);
        }

        public async Task<License?> GetByDriverIdAsync(int driverId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await Query(context).FirstOrDefaultAsync(l => l.DriverID == driverId);
        }

        public async Task<List<License>> GetAllLicensesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<List<License>> GetLicensesByDriverIdAsync(int driverId) 
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context) 
                .Where(l => l.DriverID == driverId)
                .ToListAsync();
        }

        public async Task<List<License>> GetLicensesByApplicationIdAsync(int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(l => l.ApplicationID == applicationId)
                .ToListAsync();
        }

        public async Task<List<License>> GetLicensesByLicenseClassIdAsync(int licenseClassId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(l => l.LicenseClass == licenseClassId)
                .ToListAsync();
        }

        public async Task<List<License>> GetLicensesByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(l => l.Driver.PersonID == personId)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================

        public async Task<bool> IsLicenseExistsAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Licenses
                .AnyAsync(l => l.LicenseID == id);
        }

        public async Task<bool> IsDriverHasLicenseAsync(int driverId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Licenses
                .AnyAsync(l => l.DriverID == driverId);
        }

        public async Task<bool> IsApplicationHasLicenseAsync(int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Licenses
                .AnyAsync(l => l.ApplicationID == applicationId);
        }

        // =========================
        // CREATE
        // =========================

        public async Task<int> AddLicenseAsync(License license)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.Licenses.AddAsync(license);
            await context.SaveChangesAsync();

            return license.LicenseID;
        }

        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateLicenseAsync(License license)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Licenses
                .FirstOrDefaultAsync(l => l.LicenseID == license.LicenseID);

            if (existing is null)
                return false;

            context.Entry(existing)
                .CurrentValues
                .SetValues(license);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteLicenseAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var license = await context.Licenses.FindAsync(id);

            if (license is null)
                return false;

            context.Licenses.Remove(license);

            return await context.SaveChangesAsync() > 0;
        }
    }
}