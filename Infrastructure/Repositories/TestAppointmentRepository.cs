using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TestAppointmentRepository(
    DVLDDbContext context)
    : ITestAppointmentRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

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

    public Task<TestAppointment?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<TestAppointment?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TestAppointmentID == id);

    public Task<TestAppointment?> GetForUpdateAsync(int id) =>
        id <= 0
            ? Task.FromResult<TestAppointment?>(null)
            : _context.TestAppointments
                .FirstOrDefaultAsync(
                    x => x.TestAppointmentID == id);

    public Task<List<TestAppointment>> GetAllAsync() =>
        Query()
            .AsNoTracking()
            .OrderByDescending(x => x.AppointmentDate)
            .ToListAsync();

    public Task<List<TestAppointment>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localAppId) =>
        localAppId <= 0
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId)
                .OrderBy(x => x.AppointmentDate)
                .ToListAsync();

    public Task<List<TestAppointment>>
        GetByTestTypeIdAsync(TestTypeEnum testType) =>
        !Enum.IsDefined(testType)
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.TestTypeID == (int)testType)
                .OrderByDescending(x => x.AppointmentDate)
                .ToListAsync();

    public Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(int userId) =>
        userId <= 0
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.CreatedByUserID == userId)
                .OrderByDescending(x => x.AppointmentDate)
                .ToListAsync();

    public Task<TestAppointment?> GetScheduleInfoAsync(
        int appointmentId) =>
        appointmentId <= 0
            ? Task.FromResult<TestAppointment?>(null)
            : ScheduleInfoQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TestAppointmentID == appointmentId);

    public Task<bool> HasUserConflictAsync(
        int userId,
        DateTime dateTime,
        int? excludeAppointmentId = null) =>
        userId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CreatedByUserID == userId &&
                    x.AppointmentDate == dateTime &&
                    !x.IsLocked &&
                    (!excludeAppointmentId.HasValue ||
                     x.TestAppointmentID !=
                     excludeAppointmentId.Value));

    public Task<bool> HasLocalApplicationConflictAsync(
        int localAppId,
        DateTime dateTime,
        int? excludeAppointmentId = null) =>
        localAppId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    x.AppointmentDate == dateTime &&
                    !x.IsLocked &&
                    (!excludeAppointmentId.HasValue ||
                     x.TestAppointmentID !=
                     excludeAppointmentId.Value));

    public Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId) =>
        localAppId <= 0 || testTypeId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    x.TestTypeID == testTypeId &&
                    !x.IsLocked);

    public Task<AppStatus?> GetApplicationStatusAsync(
        int localAppId) =>
        localAppId <= 0
            ? Task.FromResult<AppStatus?>(null)
            : _context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId)
                .Select(x =>
                    (AppStatus?)x.Application.ApplicationStatus)
                .FirstOrDefaultAsync();

    public async Task<HashSet<int>> GetPassedTestTypeIdsAsync(
        int localAppId)
    {
        if (localAppId <= 0)
            return [];

        var testTypeIds =
            await _context.Tests
                .AsNoTracking()
                .Where(t =>
                    t.TestAppointment
                        .LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    t.TestResult)
                .Select(t =>
                    t.TestAppointment.TestTypeID)
                .Distinct()
                .ToListAsync();

        return testTypeIds.ToHashSet();
    }

    public Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId) =>
        localAppId <= 0 || testTypeId <= 0
            ? Task.FromResult(0)
            : _context.TestAppointments
                .AsNoTracking()
                .CountAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    x.TestTypeID == testTypeId);

    public async Task AddAsync(
        TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        await _context.TestAppointments.AddAsync(
            appointment);
    }

    public void Delete(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        _context.TestAppointments.Remove(appointment);
    }

    public async Task<bool> LockLocalApplicationForSchedulingAsync(
    int localAppId)
    {
        if (localAppId <= 0)
            return false;

        var application = await _context.LocalDrivingLicenseApplications
            .FromSqlInterpolated($"""
            SELECT *
            FROM LocalDrivingLicenseApplications WITH (UPDLOCK, HOLDLOCK)
            WHERE LocalDrivingLicenseApplicationID = {localAppId}
            """)
            .FirstOrDefaultAsync();

        return application is not null;
    }
}