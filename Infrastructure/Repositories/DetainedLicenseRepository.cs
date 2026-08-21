using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories
{
    public class DetainedLicenseRepository : IDetainedLicenseRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public DetainedLicenseRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task<DetainedLicense?> GetByIdAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

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

        public async Task<List<DetainedLicense>> GetAllAsync()
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

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

        public async Task<DetainedLicense> AddAsync(
            DetainedLicense entity)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            await context.DetainedLicenses.AddAsync(entity);

            await context.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateAsync(
            DetainedLicense entity)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            context.DetainedLicenses.Update(entity);

            await context.SaveChangesAsync();
        }

        public async Task<bool> IsLicenseDetainedAsync(
            int licenseId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AsNoTracking()
                .AnyAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);
        }

        public async Task<DetainedLicense?> GetActiveDetainByLicenseIdAsync(
            int licenseId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.DetainedLicenses
                .AsNoTracking()
                .Include(d => d.License)
                    .ThenInclude(l => l.Driver)
                        .ThenInclude(dr => dr.Person)
                .Include(d => d.CreatedByUser)
                .Include(d => d.ReleasedByUser)
                .Include(d => d.ReleaseApplication)
                .FirstOrDefaultAsync(d =>
                    d.LicenseID == licenseId &&
                    !d.IsReleased);
        }
    }
}