using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class InternationalRepository : IInternationalRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public InternationalRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // BASE QUERY
        private IQueryable<InternationalLicense> Query(DVLDDbContext context)
        {
            return context.InternationalLicenses
                .Include(i => i.Application)
                .Include(i => i.Driver)
                    .ThenInclude(d => d.Person)
                .Include(i => i.IssuedUsingLocalLicense)
                .Include(i => i.CreatedByUser);
        }

        // Get all international licenses
        public async Task<IEnumerable<InternationalLicense>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .AsNoTracking()
                .ToListAsync();
        }

        // Get an international license by its ID
        public async Task<InternationalLicense?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InternationalLicenseID == id);
        }

        // Get all international licenses for a specific driver
        public async Task<IEnumerable<InternationalLicense>> GetByDriverIdAsync(int driverId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.DriverID == driverId)
                .AsNoTracking()
                .ToListAsync();
        }

        // Get an international license by application ID
        public async Task<InternationalLicense?> GetByApplicationIdAsync(int applicationId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationID == applicationId);
        }

        // Get all international licenses issued using a specific local license ID
        public async Task<IEnumerable<InternationalLicense>> GetByLocalLicenseIdAsync(int localLicenseId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.IssuedUsingLocalLicenseID == localLicenseId)
                .AsNoTracking()
                .ToListAsync();
        }
        // Check if an international license exists for a given local license ID
        public async Task<bool> ExistsByLocalLicenseAsync(int localLicenseId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.InternationalLicenses
                .AnyAsync(x => x.IssuedUsingLocalLicenseID == localLicenseId);
        }

        // Check if a driver has an active international license
        public async Task<bool> HasActiveInternationalLicenseAsync(int driverId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.InternationalLicenses
                .AnyAsync(x => x.DriverID == driverId && x.IsActive);
        }

        // Create operation
        public async Task AddAsync(InternationalLicense entity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.InternationalLicenses.AddAsync(entity);
            await context.SaveChangesAsync();
        }

        // Update operation
        public async Task UpdateAsync(InternationalLicense entity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            context.InternationalLicenses.Update(entity);
            await context.SaveChangesAsync();
        }

        // Delete operation
        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.InternationalLicenses.FindAsync(id);

            if (entity == null)
                return;

            context.InternationalLicenses.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}