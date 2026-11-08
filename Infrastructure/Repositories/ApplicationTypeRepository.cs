using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationTypeRepository
    : IApplicationTypeRepository
{
    private readonly DVLDDbContext _context;

    public ApplicationTypeRepository(
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

    public async Task<List<ApplicationType>>
        GetAllApplicationTypesAsync(
            CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationTypes
            .AsNoTracking()
            .OrderBy(x => x.ApplicationTypeId)
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ApplicationType?>
        GetApplicationTypeByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return null;

        return await _context.ApplicationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.ApplicationTypeId == id,
                cancellationToken);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateApplicationTypeAsync(
            ApplicationType appType,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            appType);

        if (appType.ApplicationTypeId <= 0)
            return false;

        var existing =
            await _context.ApplicationTypes
                .FirstOrDefaultAsync(
                    x =>
                        x.ApplicationTypeId ==
                        appType.ApplicationTypeId,
                    cancellationToken);

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(appType);

        // UnitOfWork owns SaveChangesAsync.

        return true;
    }
}
