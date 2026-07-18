using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DetainedLicenseRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public DetainedLicenseRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // Get By Id
        public async Task<DetainedLicense?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AsNoTracking()
                .Include(d => d.License)
                    .ThenInclude(l => l.Driver)
                        .ThenInclude(dr => dr.Person)
                .Include(d => d.CreatedByUser)
                .Include(d => d.ReleasedByUser)
                .Include(d => d.ReleaseApplication)
                .FirstOrDefaultAsync(d => d.DetainID == id);
        }

        // Get All
        public async Task<List<DetainedLicense>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AsNoTracking()
                .Include(d => d.License)
                    .ThenInclude(l => l.Driver)
                        .ThenInclude(dr => dr.Person)
                .Include(d => d.CreatedByUser)
                .Include(d => d.ReleasedByUser)
                .Include(d => d.ReleaseApplication)
                .OrderByDescending(d => d.DetainDate)
                .ToListAsync();
        }

        // Add
        public async Task<DetainedLicense> AddAsync(DetainedLicense entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.DetainedLicenses.AddAsync(entity);
            await context.SaveChangesAsync();

            return entity;
        }

        // Update
        public async Task UpdateAsync(DetainedLicense entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            context.DetainedLicenses.Update(entity);
            await context.SaveChangesAsync();
        }

        // Check if license is currently detained
        public async Task<bool> IsLicenseDetainedAsync(int licenseId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AnyAsync(d => d.LicenseID == licenseId && !d.IsReleased);
        }

        // Get active detention by license
        public async Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(int licenseId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AsNoTracking()
                .Include(d => d.License)
                    .ThenInclude(l => l.Driver)
                        .ThenInclude(dr => dr.Person)
                .Include(d => d.CreatedByUser)
                .FirstOrDefaultAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);
        }

        // Release license
        public async Task ReleaseAsync(
            int detainId,
            int releasedByUserId,
            int releaseApplicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var detain = await context.DetainedLicenses
                .FirstOrDefaultAsync(d => d.DetainID == detainId);

            if (detain == null)
                throw new InvalidOperationException("Detained license record was not found.");

            if (detain.IsReleased)
                throw new InvalidOperationException("This detained license has already been released.");

            detain.IsReleased = true;
            detain.ReleaseDate = DateTime.Now;
            detain.ReleasedByUserID = releasedByUserId;
            detain.ReleaseApplicationID = releaseApplicationId;

            await context.SaveChangesAsync();
        }
    }
}