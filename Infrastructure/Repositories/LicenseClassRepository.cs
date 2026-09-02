using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LicenseClassRepository : ILicenseClassRepository
{
    private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

    public LicenseClassRepository(
        IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory =
            contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<LicenseClass>>
        GetAllLicenseClassAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.LicenseClasses
            .AsNoTracking()
            .ToListAsync();
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<LicenseClass?>
        GetLicenseClassByIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.LicenseClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.LicenseClassID == id);
    }
}