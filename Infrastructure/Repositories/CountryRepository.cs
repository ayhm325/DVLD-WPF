using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CountryRepository
    : ICountryRepository
{
    private readonly DVLDDbContext _context;

    public CountryRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // GET ALL COUNTRIES
    // =========================================================

    public async Task<List<Country>>
        GetAllCountriesAsync()
    {
        return await _context.Countries
            .AsNoTracking()
            .OrderBy(c => c.CountryName)
            .ToListAsync();
    }
}