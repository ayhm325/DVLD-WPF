using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LicenseClassRepository : ILicenseClassRepository
{
    private readonly DVLDDbContext _context;

    public LicenseClassRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<LicenseClass>>
        GetAllLicenseClassAsync()
    {
        return await _context.LicenseClasses
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

        return await _context.LicenseClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.LicenseClassID == id);
    }
}
