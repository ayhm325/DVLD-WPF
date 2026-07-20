using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ApplicationTypeRepository : IApplicationTypeRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;


        public ApplicationTypeRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }



        public async Task<List<ApplicationType>> GetAllApplicationTypesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ApplicationTypes
                .AsNoTracking()
                .OrderBy(x => x.ApplicationTypeTitle)
                .ToListAsync();
        }



        public async Task<ApplicationType?> GetApplicationTypeByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.ApplicationTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationTypeId == id);
        }



        public async Task<bool> UpdateApplicationTypeAsync(
            ApplicationType appType)
        {
            using var context = await _contextFactory.CreateDbContextAsync();


            var existing = await context.ApplicationTypes
                .FindAsync(appType.ApplicationTypeId);


            if (existing is null)
                return false;


            context.Entry(existing)
                .CurrentValues
                .SetValues(appType);


            return await context.SaveChangesAsync() > 0;
        }
    }
}