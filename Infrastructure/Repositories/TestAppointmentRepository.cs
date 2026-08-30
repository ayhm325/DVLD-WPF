using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class TestAppointmentRepository
    : ITestAppointmentRepository
{
    private readonly DVLDDbContext _context;

    public TestAppointmentRepository(
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

    private IQueryable<TestAppointment>
        Query()
    {
        return _context.TestAppointments
            .Include(x => x.TestType)
            .Include(x =>
                x.LocalDrivingLicenseApplication)
            .Include(x => x.User)
            .Include(x => x.Test)
            .Include(x => x.RetakeTestApplication);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<TestAppointment?>
        GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                x =>
                    x.TestAppointmentID == id);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<TestAppointment>>
        GetAllAsync()
    {
        return await Query()
            .AsNoTracking()
            .ToListAsync();
    }

    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    public async Task<List<TestAppointment>>
        GetByApplicationIdAsync(
            int applicationId)
    {
        if (applicationId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(
                x =>
                    x.LocalDrivingLicenseApplicationID ==
                    applicationId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY TEST TYPE
    // =========================================================

    public async Task<List<TestAppointment>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType)
    {
        return await Query()
            .AsNoTracking()
            .Where(
                x =>
                    x.TestTypeID ==
                    (int)testType)
            .ToListAsync();
    }

    // =========================================================
    // GET BY USER
    // =========================================================

    public async Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(
            int userId)
    {
        if (userId <= 0)
            return [];

        return await Query()
            .AsNoTracking()
            .Where(
                x =>
                    x.CreatedByUserID ==
                    userId)
            .ToListAsync();
    }

    // =========================================================
    // GET SCHEDULE INFO
    // =========================================================

    public async Task<TestAppointment?>
        GetScheduleInfoAsync(
            int testAppointmentId)
    {
        if (testAppointmentId <= 0)
            return null;

        return await _context.TestAppointments
            .AsNoTracking()
            .Include(x => x.TestType)
            .Include(x =>
                x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.Application)
                    .ThenInclude(a => a.Person)
            .Include(x =>
                x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.Application)
                    .ThenInclude(a => a.ApplicationType)
            .Include(x =>
                x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.LicenseClass)
            .FirstOrDefaultAsync(
                x =>
                    x.TestAppointmentID ==
                    testAppointmentId);
    }

    // =========================================================
    // EXISTS
    // =========================================================

    public async Task<bool>
        ExistsAsync(
            Expression<Func<TestAppointment, bool>>
                predicate)
    {
        ArgumentNullException.ThrowIfNull(
            predicate);

        return await _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(predicate);
    }

    // =========================================================
    // CONFLICT
    // =========================================================

    public async Task<bool>
        HasConflictAsync(
            int localAppId,
            int testTypeId,
            DateTime dateTime,
            int? excludeAppointmentId = null)
    {
        if (localAppId <= 0 ||
            testTypeId <= 0)
        {
            return false;
        }

        var query =
            _context.TestAppointments
                .AsNoTracking()
                .Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        localAppId &&
                        x.TestTypeID ==
                        testTypeId &&
                        x.AppointmentDate ==
                        dateTime);

        if (excludeAppointmentId.HasValue)
        {
            query =
                query.Where(
                    x =>
                        x.TestAppointmentID !=
                        excludeAppointmentId.Value);
        }

        return await query.AnyAsync();
    }

    // =========================================================
    // USER CONFLICT
    // =========================================================

    public async Task<bool>
        HasUserConflictAsync(
            int userId,
            DateTime dateTime)
    {
        if (userId <= 0)
            return false;

        return await _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.CreatedByUserID ==
                    userId &&
                    x.AppointmentDate ==
                    dateTime);
    }

    // =========================================================
    // APPLICATION CONFLICT
    // =========================================================

    public async Task<bool>
        HasApplicationConflictAsync(
            int applicationId,
            DateTime dateTime)
    {
        if (applicationId <= 0)
            return false;

        return await _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.LocalDrivingLicenseApplicationID ==
                    applicationId &&
                    x.AppointmentDate ==
                    dateTime);
    }

    // =========================================================
    // ALREADY SCHEDULED
    // =========================================================

    public async Task<bool>
        IsAppointmentAlreadyScheduledAsync(
            int localAppId,
            int testTypeId)
    {
        if (localAppId <= 0 ||
            testTypeId <= 0)
        {
            return false;
        }

        var hasPendingAppointment =
            await _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(
                    a =>
                        a.LocalDrivingLicenseApplicationID ==
                        localAppId &&
                        a.TestTypeID ==
                        testTypeId &&
                        !a.IsLocked);

        if (hasPendingAppointment)
            return true;

        return await _context.Tests
            .AsNoTracking()
            .AnyAsync(
                t =>
                    t.TestAppointment != null &&
                    t.TestAppointment
                        .LocalDrivingLicenseApplicationID ==
                        localAppId &&
                    t.TestAppointment
                        .TestTypeID ==
                        testTypeId &&
                    t.TestResult);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<bool>
        AddAsync(
            TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(
            appointment);

        await _context.TestAppointments
            .AddAsync(appointment);

        // No SaveChangesAsync.

        return true;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateAsync(
            TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(
            appointment);

        if (appointment.TestAppointmentID <= 0)
            return false;

        var existing =
            await _context.TestAppointments
                .FirstOrDefaultAsync(
                    x =>
                        x.TestAppointmentID ==
                        appointment.TestAppointmentID);

        if (existing is null)
            return false;

        existing.TestTypeID =
            appointment.TestTypeID;

        existing.LocalDrivingLicenseApplicationID =
            appointment.LocalDrivingLicenseApplicationID;

        existing.AppointmentDate =
            appointment.AppointmentDate;

        existing.PaidFees =
            appointment.PaidFees;

        existing.CreatedByUserID =
            appointment.CreatedByUserID;

        existing.IsLocked =
            appointment.IsLocked;

        existing.RetakeTestApplicationID =
            appointment.RetakeTestApplicationID;

        // No SaveChangesAsync.

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task
        DeleteAsync(int id)
    {
        if (id <= 0)
            return;

        var entity =
            await _context.TestAppointments
                .FirstOrDefaultAsync(
                    x =>
                        x.TestAppointmentID ==
                        id);

        if (entity is null)
            return;

        _context.TestAppointments
            .Remove(entity);

        // No SaveChangesAsync.
    }
}