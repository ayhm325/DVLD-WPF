using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationRepository
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

    public async Task<ApplicationD?>
        GetApplicationByIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                a =>
                    a.ApplicationID == id);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<ApplicationD>>
        GetAllApplicationsAsync()
    {
        return await Query()
            .ToListAsync();
    }


    // =========================================================
    // GET BY PERSON
    // =========================================================

    public async Task<List<ApplicationD>>
        GetApplicationsByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.ApplicantPersonID == personId)
            .ToListAsync();
    }


    // =========================================================
    // GET BY APPLICATION TYPE
    // =========================================================

    public async Task<List<ApplicationD>>
        GetApplicationsByApplicationTypeIdAsync(
            int applicationTypeId)
    {
        if (applicationTypeId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.ApplicationTypeID ==
                applicationTypeId)
            .ToListAsync();
    }


    // =========================================================
    // GET BY USER
    // =========================================================

    public async Task<List<ApplicationD>>
        GetApplicationsByUserIdAsync(
            int userId)
    {
        if (userId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.CreatedByUserID == userId)
            .ToListAsync();
    }


    // =========================================================
    // GET BY STATUS
    // =========================================================

    public async Task<List<ApplicationD>>
        GetApplicationsByStatusAsync(
            AppStatus status)
    {
        if (!Enum.IsDefined(status))
            return [];

        return await Query()
            .Where(a =>
                a.ApplicationStatus == status)
            .ToListAsync();
    }


    // =========================================================
    // CHECK APPLICATION EXISTS
    // =========================================================

    public async Task<bool>
        IsApplicationExistsByIdAsync(
            int id)
    {
        if (id <= 0)
            return false;

        return await _context.Applications
            .AsNoTracking()
            .AnyAsync(a =>
                a.ApplicationID == id);
    }


    // =========================================================
    // CHECK ACTIVE APPLICATION
    // =========================================================

    public async Task<bool>
        IsPersonHasActiveApplicationAsync(
            int personId)
    {
        if (personId <= 0)
            return false;

        return await _context.Applications
            .AsNoTracking()
            .AnyAsync(a =>
                a.ApplicantPersonID == personId &&
                a.ApplicationStatus ==
                    AppStatus.New);
    }


    // =========================================================
    // CHECK ACTIVE APPLICATION OF TYPE
    // =========================================================

    public async Task<bool>
        IsPersonHasActiveApplicationOfTypeAsync(
            int personId,
            int applicationTypeId)
    {
        if (personId <= 0 ||
            applicationTypeId <= 0)
        {
            return false;
        }

        return await _context.Applications
            .AsNoTracking()
            .AnyAsync(a =>
                a.ApplicantPersonID == personId &&
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
                .Where(ldla =>
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
                .Select(ldla =>
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

        // No SaveChanges here.
        //
        // UnitOfWork controls persistence.
        //
        // application.ApplicationID
        // will be generated after SaveChangesAsync().
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateApplicationAsync(
            ApplicationD application)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        if (application.ApplicationID <= 0)
            return false;

        var existing =
            await _context.Applications
                .FirstOrDefaultAsync(
                    a =>
                        a.ApplicationID ==
                        application.ApplicationID);

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(application);

        // No SaveChanges here.
        return true;
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteApplicationAsync(
            int id)
    {
        if (id <= 0)
            return false;

        var application =
            await _context.Applications
                .FirstOrDefaultAsync(
                    a =>
                        a.ApplicationID == id);

        if (application is null)
            return false;

        _context.Applications
            .Remove(application);

        // No SaveChanges here.
        return true;
    }
}