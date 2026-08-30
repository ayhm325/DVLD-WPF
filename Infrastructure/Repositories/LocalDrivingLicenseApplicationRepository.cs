using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class LocalDrivingLicenseApplicationRepository
    : ILocalDrivingLicenseApplicationRepository
{
    private readonly DVLDDbContext _context;

    public LocalDrivingLicenseApplicationRepository(
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

    private IQueryable<LocalDrivingLicenseApplication>
        Query()
    {
        return _context
            .LocalDrivingLicenseApplications
            .AsNoTracking()
            .Include(a =>
                a.Application)
                .ThenInclude(app =>
                    app.Person)
            .Include(a =>
                a.LicenseClass);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<
        List<LocalDrivingLicenseApplication>>
        GetAllAsync()
    {
        return await Query()
            .ToListAsync();
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<
        LocalDrivingLicenseApplication?>
        GetByIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        return await _context
            .LocalDrivingLicenseApplications
            .Include(a =>
                a.Application)
                .ThenInclude(app =>
                    app.Person)
            .Include(a =>
                a.LicenseClass)
            .FirstOrDefaultAsync(
                a =>
                    a.LocalDrivingLicenseApplicationID ==
                    id);
    }

    // =========================================================
    // GET BY PERSON
    // =========================================================

    public async Task<
        List<LocalDrivingLicenseApplication>>
        GetByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.Application
                    .ApplicantPersonID ==
                personId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    public async Task<
        List<LocalDrivingLicenseApplication>>
        GetByApplicationIdAsync(
            int applicationId)
    {
        if (applicationId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.ApplicationID ==
                applicationId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY LICENSE CLASS
    // =========================================================

    public async Task<
        List<LocalDrivingLicenseApplication>>
        GetByLicenseClassIdAsync(
            int licenseClassId)
    {
        if (licenseClassId <= 0)
            return [];

        return await Query()
            .Where(a =>
                a.LicenseClassID ==
                licenseClassId)
            .ToListAsync();
    }

    // =========================================================
    // GET PASSED TEST COUNT
    // =========================================================

    public async Task<int>
        GetPassedTestCountAsync(
            int localAppId)
    {
        if (localAppId <= 0)
            return 0;

        return await _context.Tests
            .CountAsync(t =>
                t.TestAppointment != null &&
                t.TestAppointment
                    .LocalDrivingLicenseApplicationID ==
                    localAppId &&
                t.TestResult == true);
    }

    // =========================================================
    // GET APPLICATION ID
    // =========================================================

    public async Task<int?>
        GetApplicationIdByLocalIdAsync(
            int localId)
    {
        if (localId <= 0)
            return null;

        var applicationId =
            await _context
                .LocalDrivingLicenseApplications
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localId)
                .Select(x =>
                    (int?)x.ApplicationID)
                .FirstOrDefaultAsync();

        return applicationId;
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<int>
        CreateLocalDrivingLicenseApplicationAsync(
            LocalDrivingLicenseApplication entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        await _context
            .LocalDrivingLicenseApplications
            .AddAsync(entity);

        // IMPORTANT:
        // Do NOT call SaveChangesAsync here.
        //
        // UnitOfWork owns persistence.
        //
        // The generated ID will be available
        // after UnitOfWork.SaveChangesAsync().

        return entity.LocalDrivingLicenseApplicationID;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateAsync(
            LocalDrivingLicenseApplication entity)
    {
        ArgumentNullException.ThrowIfNull(
            entity);

        if (entity.LocalDrivingLicenseApplicationID <= 0)
            return false;

        var existing =
            await _context
                .LocalDrivingLicenseApplications
                .FirstOrDefaultAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        entity.LocalDrivingLicenseApplicationID);

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(entity);

        // No SaveChangesAsync here.

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteAsync(
            int id)
    {
        if (id <= 0)
            return false;

        var existing =
            await _context
                .LocalDrivingLicenseApplications
                .FirstOrDefaultAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        id);

        if (existing is null)
            return false;

        _context
            .LocalDrivingLicenseApplications
            .Remove(existing);

        // No SaveChangesAsync here.

        return true;
    }
}