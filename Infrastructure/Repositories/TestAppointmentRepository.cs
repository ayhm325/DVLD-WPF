using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public sealed class TestAppointmentRepository
    : ITestAppointmentRepository
{
    private readonly DVLDDbContext _context;

    public TestAppointmentRepository(DVLDDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    // =========================================================
    // QUERIES
    // =========================================================

    private IQueryable<TestAppointment> Query() =>
        _context.TestAppointments
            .Include(x => x.TestType)
            .Include(x => x.LocalDrivingLicenseApplication)
            .Include(x => x.User)
            .Include(x => x.Test)
            .Include(x => x.RetakeTestApplication);

    private IQueryable<TestAppointment> ScheduleInfoQuery() =>
        _context.TestAppointments
            .Include(x => x.TestType)
            .Include(x => x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.Application)
                    .ThenInclude(x => x.Person)
            .Include(x => x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.Application)
                    .ThenInclude(x => x.ApplicationType)
            .Include(x => x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.LicenseClass);

    // =========================================================
    // GET
    // =========================================================

    public async Task<TestAppointment?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TestAppointmentID == id);
    }

    public async Task<TestAppointment?> GetForUpdateAsync(int id)
    {
        if (id <= 0)
            return null;

        return await _context.TestAppointments
            .FirstOrDefaultAsync(x =>
                x.TestAppointmentID == id);
    }

    public Task<List<TestAppointment>> GetAllAsync() =>
        Query()
            .AsNoTracking()
            .ToListAsync();

    public Task<List<TestAppointment>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localDrivingLicenseApplicationId)
    {
        if (localDrivingLicenseApplicationId <= 0)
            return Task.FromResult<List<TestAppointment>>([]);

        return Query()
            .AsNoTracking()
            .Where(x =>
                x.LocalDrivingLicenseApplicationID ==
                localDrivingLicenseApplicationId)
            .ToListAsync();
    }

    public Task<List<TestAppointment>>
        GetByTestTypeIdAsync(TestTypeEnum testType)
    {
        if (!Enum.IsDefined(testType))
            return Task.FromResult<List<TestAppointment>>([]);

        return Query()
            .AsNoTracking()
            .Where(x =>
                x.TestTypeID == (int)testType)
            .ToListAsync();
    }

    public Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(int userId)
    {
        if (userId <= 0)
            return Task.FromResult<List<TestAppointment>>([]);

        return Query()
            .AsNoTracking()
            .Where(x =>
                x.CreatedByUserID == userId)
            .ToListAsync();
    }

    public async Task<TestAppointment?> GetScheduleInfoAsync(
        int testAppointmentId)
    {
        if (testAppointmentId <= 0)
            return null;

        return await ScheduleInfoQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TestAppointmentID == testAppointmentId);
    }

    // =========================================================
    // CHECKS
    // =========================================================

    public Task<bool> ExistsAsync(
        Expression<Func<TestAppointment, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(predicate);
    }

    public Task<bool> HasConflictAsync(
        int localAppId,
        int testTypeId,
        DateTime dateTime,
        int? excludeAppointmentId = null)
    {
        if (localAppId <= 0 || testTypeId <= 0)
            return Task.FromResult(false);

        return _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(x =>
                x.LocalDrivingLicenseApplicationID == localAppId &&
                x.TestTypeID == testTypeId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue ||
                 x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public Task<bool> HasUserConflictAsync(
        int userId,
        DateTime dateTime,
        int? excludeAppointmentId = null)
    {
        if (userId <= 0)
            return Task.FromResult(false);

        return _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(x =>
                x.CreatedByUserID == userId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue ||
                 x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public Task<bool> HasLocalApplicationConflictAsync(
        int localAppId,
        DateTime dateTime,
        int? excludeAppointmentId = null)
    {
        if (localAppId <= 0)
            return Task.FromResult(false);

        return _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(x =>
                x.LocalDrivingLicenseApplicationID == localAppId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue ||
                 x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public async Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId)
    {
        if (localAppId <= 0 || testTypeId <= 0)
            return false;

        var hasPendingAppointment =
            await _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID == localAppId &&
                    x.TestTypeID == testTypeId &&
                    !x.IsLocked);

        if (hasPendingAppointment)
            return true;

        return await _context.Tests
            .AsNoTracking()
            .AnyAsync(x =>
                x.TestAppointment != null &&
                x.TestAppointment.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                x.TestAppointment.TestTypeID == testTypeId &&
                x.TestResult);
    }

    public async Task<AppStatus?> GetApplicationStatusAsync(
        int localAppId)
    {
        if (localAppId <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .Where(x =>
                x.LocalDrivingLicenseApplicationID == localAppId)
            .Select(x =>
                (AppStatus?)x.Application.ApplicationStatus)
            .FirstOrDefaultAsync();
    }

    public async Task<HashSet<int>> GetPassedTestTypeIdsAsync(int localAppId)
    {
        if (localAppId <= 0)
            return [];

        var testTypeIds = await _context.Tests
            .AsNoTracking()
            .Where(t =>
                t.TestAppointment.LocalDrivingLicenseApplicationID == localAppId &&
                t.TestResult)
            .Select(t => t.TestAppointment.TestTypeID)
            .Distinct()
            .ToListAsync();

        return testTypeIds.ToHashSet();
    }

    // =========================================================
    // COMMANDS
    // =========================================================

    public async Task AddAsync(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        await _context.TestAppointments.AddAsync(appointment);
    }

    public void Delete(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        _context.TestAppointments.Remove(appointment);
    }
}