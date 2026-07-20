using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;


        public CountryRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }



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