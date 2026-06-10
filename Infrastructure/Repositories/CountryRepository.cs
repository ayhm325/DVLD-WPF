using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Repositories
{
    public class CountryRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public CountryRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<List<Country>> GetAllCountriesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Countries
                .AsNoTracking()
                .OrderBy(c => c.CountryName)
                .ToListAsync();
        }
    }
}