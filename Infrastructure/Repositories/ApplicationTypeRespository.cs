using Domain.Entities;
using DVLD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class ApplicationTypeRespository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public ApplicationTypeRespository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<List<ApplicationType>> GetAllApplicationTypesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ApplicationTypes
                                .ToListAsync();
        }

        public async Task<ApplicationType?> GetApplicationTypeByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.ApplicationTypes
                .FirstOrDefaultAsync(a => a.ApplicationTypeId == id);
        }


        // =========================
        // UPDATE OPERATION
        // =========================
        public async Task<bool> UpdateApplicationTypeAsync(ApplicationType apptype)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.ApplicationTypes.FindAsync(apptype.ApplicationTypeId);
            if (existing is null) return false;

            context.Entry(existing).CurrentValues.SetValues(apptype);
            return await context.SaveChangesAsync() > 0;
        }
    }

}
