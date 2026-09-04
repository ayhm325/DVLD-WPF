using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ApplicationRepository
    : IApplicationRepository
{
    private readonly DVLDDbContext _context;

    public ApplicationRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
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
        int id)
    {
        if (id <= 0)
            return Task.FromResult<ApplicationD?>(
                null);

        return Query()
            .FirstOrDefaultAsync(
                a => a.ApplicationID == id);
    }


    // =========================================================
    // GET FOR UPDATE
    // =========================================================

    public Task<ApplicationD?> GetApplicationForUpdateAsync(
        int id)
    {
        if (id <= 0)
            return Task.FromResult<ApplicationD?>(
                null);

        return _context.Applications
            .FirstOrDefaultAsync(
                a => a.ApplicationID == id);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public Task<List<ApplicationD>>
        GetAllApplicationsAsync()
    {
        return Query()
            .OrderByDescending(
                a => a.ApplicationID)
            .ToListAsync();
    }


    // =========================================================
    // GET BY PERSON
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByPersonIdAsync(
            int personId)
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
            .ToListAsync();
    }


    // =========================================================
    // GET BY APPLICATION TYPE
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByApplicationTypeIdAsync(
            int applicationTypeId)
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
            .ToListAsync();
    }


    // =========================================================
    // GET BY USER
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByUserIdAsync(
            int userId)
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
            .ToListAsync();
    }


    // =========================================================
    // GET BY STATUS
    // =========================================================

    public Task<List<ApplicationD>>
        GetApplicationsByStatusAsync(
            AppStatus status)
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
            .ToListAsync();
    }


    // =========================================================
    // EXISTS BY ID
    // =========================================================

    public Task<bool>
        IsApplicationExistsByIdAsync(
            int id)
    {
        if (id <= 0)
            return Task.FromResult(false);

        return _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicationID == id);
    }


    // =========================================================
    // PERSON HAS ACTIVE APPLICATION
    // =========================================================

    public Task<bool>
        IsPersonHasActiveApplicationAsync(
            int personId)
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
                        AppStatus.New);
    }


    // =========================================================
    // PERSON HAS ACTIVE APPLICATION OF TYPE
    // =========================================================

    public Task<bool>
        IsPersonHasActiveApplicationOfTypeAsync(
            int personId,
            int applicationTypeId)
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
                        AppStatus.New);
    }


    // =========================================================
    // CHECK DUPLICATE LOCAL DRIVING APPLICATION
    // =========================================================

    public async Task<int?>
        HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId)
    {
        if (personId <= 0 ||
            licenseClassId <= 0)
        {
            return null;
        }

        var applicationId =
            await _context
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .Where(
                    ldla =>
                        ldla.Application
                            .ApplicantPersonID ==
                            personId &&

                        ldla.LicenseClassID ==
                            licenseClassId &&

                        (
                            ldla.Application
                                .ApplicationStatus ==
                                AppStatus.New ||

                            ldla.Application
                                .ApplicationStatus ==
                                AppStatus.Completed
                        ))
                .Select(
                    ldla =>
                        ldla.ApplicationID)
                .FirstOrDefaultAsync();

        return applicationId == 0
            ? null
            : applicationId;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task AddNewApplicationAsync(
        ApplicationD application)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        await _context.Applications
            .AddAsync(application);

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
