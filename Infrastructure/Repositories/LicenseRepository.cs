using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LicenseRepository : ILicenseRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;


        public LicenseRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
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
                .Include(l => l.Application)
                .Include(l => l.Driver)
                    .ThenInclude(d => d.Person)
                .Include(l => l.LicenseClassInfo)
                .Include(l => l.CreatedByUser);
        }



        // =========================
        // GET
        // =========================

        public async Task<License?> GetLicenseByIdAsync(int id)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .FirstOrDefaultAsync(l =>
                    l.LicenseID == id);
        }



        public async Task<List<License>> GetAllLicensesAsync()
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .ToListAsync();
        }



        public async Task<License?> GetByDriverIdAsync(int driverId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .FirstOrDefaultAsync(l =>
                    l.DriverID == driverId);
        }



        public async Task<List<License>> GetLicensesByDriverIdAsync(
            int driverId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .Where(l =>
                    l.DriverID == driverId)
                .ToListAsync();
        }



        public async Task<List<License>> GetLicensesByApplicationIdAsync(
            int applicationId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .Where(l =>
                    l.ApplicationID == applicationId)
                .ToListAsync();
        }



        public async Task<List<License>> GetLicensesByLicenseClassIdAsync(
            int licenseClassId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .Where(l =>
                    l.LicenseClass == licenseClassId)
                .ToListAsync();
        }



        public async Task<List<License>> GetLicensesByPersonIdAsync(
            int personId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .AsNoTracking()
                .Where(l =>
                    l.Driver.PersonID == personId)
                .ToListAsync();
        }



        // =========================
        // EXISTS
        // =========================

        public async Task<bool> IsLicenseExistsAsync(int id)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Licenses
                .AnyAsync(l =>
                    l.LicenseID == id);
        }



        public async Task<bool> IsDriverHasLicenseAsync(int driverId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Licenses
                .AnyAsync(l =>
                    l.DriverID == driverId);
        }



        public async Task<bool> IsApplicationHasLicenseAsync(
            int applicationId)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Licenses
                .AnyAsync(l =>
                    l.ApplicationID == applicationId);
        }



        // =========================
        // CREATE
        // =========================

        public async Task<int> AddLicenseAsync(
            License license)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            await context.Licenses.AddAsync(license);

            await context.SaveChangesAsync();


            return license.LicenseID;
        }



        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateLicenseAsync(
            License license)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            var existing =
                await context.Licenses
                    .FirstOrDefaultAsync(l =>
                        l.LicenseID == license.LicenseID);


            if (existing == null)
                return false;


            context.Entry(existing)
                .CurrentValues
                .SetValues(license);


            return await context.SaveChangesAsync() > 0;
        }



        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteLicenseAsync(
            int id)
        {
            await using var context =
                await _contextFactory.CreateDbContextAsync();


            var license =
                await context.Licenses
                    .FindAsync(id);


            if (license == null)
                return false;


            context.Licenses.Remove(license);


            return await context.SaveChangesAsync() > 0;
        }
    }
}