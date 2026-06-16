using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

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

        // Get by ID
        public async Task<DetainedLicense?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<DetainedLicense>()
                .Include(d => d.License)
                .Include(d => d.CreatedByUser)
                .Include(d => d.ReleasedByUser)
                .Include(d => d.ReleaseApplication)
                .FirstOrDefaultAsync(d => d.DetainID == id);
        }

        // Get all
        public async Task<List<DetainedLicense>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<DetainedLicense>()
                .Include(d => d.License)
                .ToListAsync();
        }

        // Add new detain record
        public async Task<DetainedLicense> AddAsync(DetainedLicense entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.Set<DetainedLicense>().AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        // Update (release / fees / etc.)
        public async Task UpdateAsync(DetainedLicense entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Set<DetainedLicense>().Update(entity);
            await context.SaveChangesAsync();
        }

        // Check if license is detained and not released
        public async Task<bool> IsLicenseDetainedAsync(int licenseId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<DetainedLicense>()
                .AnyAsync(d => d.LicenseID == licenseId && !d.IsReleased);
        }

        // Release license
        public async Task ReleaseAsync(int detainId, int releasedByUserId, int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var detain = await context.Set<DetainedLicense>()
                .FirstOrDefaultAsync(d => d.DetainID == detainId);

            if (detain == null)
                throw new Exception("Detain record not found.");

            detain.IsReleased = true;
            detain.ReleaseDate = DateTime.Now;
            detain.ReleasedByUserID = releasedByUserId;
            detain.ReleaseApplicationID = applicationId;

            await context.SaveChangesAsync();
        }
    }
}