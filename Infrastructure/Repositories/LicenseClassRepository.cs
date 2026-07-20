using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LicenseClassRepository : ILicenseClassRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public LicenseClassRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<List<LicenseClass>> GetAllLicenseClassAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.LicenseClasses
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<LicenseClass?> GetLicenseClassByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.LicenseClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.LicenseClassID == id);
        }
    }
}