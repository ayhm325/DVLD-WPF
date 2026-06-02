using Microsoft.EntityFrameworkCore;
using DVLD.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Repositories
{
    public class CountryRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public CountryRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task<List<Country>> GetAllCountriesAsync()
        {
            // استخدام الـ Factory لإنشاء Context متزامن مع الـ Async
            using var context = await _contextFactory.CreateDbContextAsync();

            // استدعاء EF Core Native Async
            return await context.Countries
                                .OrderBy(c => c.CountryName) // اختيار اختياري للترتيب
                                .ToListAsync();
        }
    }
}