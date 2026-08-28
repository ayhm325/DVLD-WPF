using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationRepository
    : IApplicationRepository
{
    private readonly IDbContextFactory<DVLDDbContext> _contextFactory;


    public ApplicationRepository(
        IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory =
            contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }


    // =========================================================
    // BASE QUERY
    // =========================================================

    private static IQueryable<ApplicationD> Query(
        DVLDDbContext context)
    {
        return context.Applications
            .AsNoTracking()
            .Include(a => a.Person)
            .Include(a => a.ApplicationType)
            .Include(a => a.CreatedByUser);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ApplicationD?>
        GetApplicationByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .FirstOrDefaultAsync(
                a => a.ApplicationID == id);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<ApplicationD>>
        GetAllApplicationsAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
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
            return new List<ApplicationD>();

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
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
            return new List<ApplicationD>();

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .Where(a =>
                a.ApplicationTypeID == applicationTypeId)
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
            return new List<ApplicationD>();

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
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
            return new List<ApplicationD>();

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await Query(context)
            .Where(a =>
                a.ApplicationStatus == status)
            .ToListAsync();
    }


    // =========================================================
    // CHECK APPLICATION EXISTS
    // =========================================================

    public async Task<bool>
        IsApplicationExistsByIdAsync(int id)
    {
        if (id <= 0)
            return false;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Applications
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Applications
            .AsNoTracking()
            .AnyAsync(a =>
                a.ApplicantPersonID == personId &&
                a.ApplicationStatus == AppStatus.New);
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        return await context.Applications
            .AsNoTracking()
            .AnyAsync(a =>
                a.ApplicantPersonID == personId &&
                a.ApplicationTypeID == applicationTypeId &&
                a.ApplicationStatus == AppStatus.New);
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var applicationId =
            await context
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .Where(ldla =>
                    ldla.Application.ApplicantPersonID ==
                        personId &&

                    ldla.LicenseClassID ==
                        licenseClassId &&

                    (
                        ldla.Application.ApplicationStatus ==
                            AppStatus.New ||

                        ldla.Application.ApplicationStatus ==
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

    public async Task<int>
        AddNewApplicationAsync(
            ApplicationD application)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        await context.Applications
            .AddAsync(application);

        await context.SaveChangesAsync();

        return application.ApplicationID;
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var existing =
            await context.Applications
                .FirstOrDefaultAsync(
                    a =>
                        a.ApplicationID ==
                        application.ApplicationID);

        if (existing is null)
            return false;

        context.Entry(existing)
            .CurrentValues
            .SetValues(application);

        return await context.SaveChangesAsync() > 0;
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteApplicationAsync(int id)
    {
        if (id <= 0)
            return false;

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var application =
            await context.Applications
                .FirstOrDefaultAsync(
                    a =>
                        a.ApplicationID == id);

        if (application is null)
            return false;

        context.Applications.Remove(
            application);

        return await context.SaveChangesAsync() > 0;
    }
}