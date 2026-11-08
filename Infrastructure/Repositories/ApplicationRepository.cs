using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly DVLDDbContext _context;

    public ApplicationRepository(DVLDDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(
                nameof(context));
    }


    // =========================================================
    // BASE QUERY
    // =========================================================

    private IQueryable<ApplicationD> Query()
    {
        return _context.Applications
            .AsNoTracking()
            .Include(a => a.Person)
            .Include(a => a.ApplicationType)
            .Include(a => a.CreatedByUser);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public Task<ApplicationD?> GetApplicationByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return Task.FromResult<ApplicationD?>(
                null);

        return Query()
            .FirstOrDefaultAsync(
                a => a.ApplicationID == id,
                cancellationToken);
    }


    // =========================================================
    // GET FOR UPDATE
    // =========================================================

    public Task<ApplicationD?> GetApplicationForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return Task.FromResult<ApplicationD?>(
                null);

        return _context.Applications
            .FirstOrDefaultAsync(
                a => a.ApplicationID == id,
                cancellationToken);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public Task<List<ApplicationD>>
        GetAllApplicationsAsync(
            CancellationToken cancellationToken = default)
    {
        return Query()
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // GET BY PERSON
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByPersonIdAsync(
            int personId,
            CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
            return Task.FromResult(
                new List<ApplicationD>());

        return Query()
            .Where(
                a =>
                    a.ApplicantPersonID ==
                    personId)
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // GET BY APPLICATION TYPE
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByApplicationTypeIdAsync(
            int applicationTypeId,
            CancellationToken cancellationToken = default)
    {
        if (applicationTypeId <= 0)
            return Task.FromResult(
                new List<ApplicationD>());

        return Query()
            .Where(
                a =>
                    a.ApplicationTypeID ==
                    applicationTypeId)
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // GET BY USER
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return Task.FromResult(
                new List<ApplicationD>());

        return Query()
            .Where(
                a =>
                    a.CreatedByUserID ==
                    userId)
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync(cancellationToken);
    }


    // =========================================================
    // GET BY STATUS
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByStatusAsync(
            AppStatus status,
            CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
            return Task.FromResult(
                new List<ApplicationD>());

        return Query()
            .Where(
                a =>
                    a.ApplicationStatus ==
                    status)
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync(cancellationToken);
    }

    // =========================================================
    // GET FOR ISSUANCE
    // =========================================================

    public Task<ApplicationD?> GetApplicationForIssuanceAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return Task.FromResult<ApplicationD?>(
                null);

        return _context.Applications
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [Applications] WITH (UPDLOCK, ROWLOCK)
                WHERE [ApplicationID] = {id}
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    // =========================================================
    // EXISTS BY ID
    // =========================================================

    public Task<bool>
        IsApplicationExistsByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return Task.FromResult(false);

        return _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicationID == id,
                cancellationToken);
    }


    // =========================================================
    // PERSON HAS ACTIVE APPLICATION
    // =========================================================

    public Task<bool>
        IsPersonHasActiveApplicationAsync(
            int personId,
            CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
            return Task.FromResult(false);

        return _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicantPersonID ==
                        personId &&
                    a.ApplicationStatus ==
                        AppStatus.New,
                cancellationToken);
    }


    // =========================================================
    // PERSON HAS ACTIVE APPLICATION OF TYPE
    // =========================================================

    public Task<bool>
        IsPersonHasActiveApplicationOfTypeAsync(
            int personId,
            int applicationTypeId,
            CancellationToken cancellationToken = default)
    {
        if (personId <= 0 ||
            applicationTypeId <= 0)
        {
            return Task.FromResult(false);
        }

        return _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicantPersonID ==
                        personId &&
                    a.ApplicationTypeID ==
                        applicationTypeId &&
                    a.ApplicationStatus ==
                        AppStatus.New,
                cancellationToken);
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task AddNewApplicationAsync(
        ApplicationD application,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        await _context.Applications
            .AddAsync(application, cancellationToken);

        // No SaveChangesAsync here.
        // UnitOfWork owns persistence.
    }


    // =========================================================
    // DELETE
    // =========================================================

    public void DeleteApplication(
        ApplicationD application)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        _context.Applications
            .Remove(application);

        // No SaveChangesAsync here.
        // UnitOfWork owns persistence.
    }
}
