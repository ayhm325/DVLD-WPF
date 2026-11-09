using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

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
                .ThenInclude(x => x.Application)
                    .ThenInclude(x => x.Person)
            .Include(x => x.LocalDrivingLicenseApplication)
                .ThenInclude(x => x.LicenseClass)
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

    public Task<TestAppointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<TestAppointment?>(null)
            : Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TestAppointmentID == id,
                    cancellationToken);

    public Task<TestAppointment?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
    id <= 0
        ? Task.FromResult<TestAppointment?>(null)
        : _context.TestAppointments
            .FromSqlInterpolated($"""
                SELECT *
                FROM TestAppointments WITH (UPDLOCK, HOLDLOCK)
                WHERE TestAppointmentID = {id}
                """)
            .Include(x => x.Test)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<TestAppointment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Query()
            .AsNoTracking()
            .OrderByDescending(x => x.AppointmentDate)
            .ToListAsync(cancellationToken);

    public Task<List<TestAppointment>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localAppId,
            CancellationToken cancellationToken = default) =>
        localAppId <= 0
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId)
                .OrderBy(x => x.AppointmentDate)
                .ToListAsync(cancellationToken);

    public Task<List<TestAppointment>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType,
            CancellationToken cancellationToken = default) =>
        !Enum.IsDefined(testType)
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.TestTypeID == (int)testType)
                .OrderByDescending(x => x.AppointmentDate)
                .ToListAsync(cancellationToken);

    public Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
        userId <= 0
            ? Task.FromResult<List<TestAppointment>>([])
            : Query()
                .AsNoTracking()
                .Where(x =>
                    x.CreatedByUserID == userId)
                .OrderByDescending(x => x.AppointmentDate)
                .ToListAsync(cancellationToken);

    public Task<TestAppointment?> GetScheduleInfoAsync(
        int appointmentId,
        CancellationToken cancellationToken = default) =>
        appointmentId <= 0
            ? Task.FromResult<TestAppointment?>(null)
            : ScheduleInfoQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TestAppointmentID == appointmentId,
                    cancellationToken);

    public Task<bool> HasUserConflictAsync(
        int userId,
        DateTime dateTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default) =>
        userId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CreatedByUserID == userId &&
                    x.AppointmentDate == dateTime &&
                    x.Status != AppointmentStatus.Cancelled &&
                    x.Status != null &&
                    !x.IsLocked &&
                    (!excludeAppointmentId.HasValue ||
                     x.TestAppointmentID !=
                     excludeAppointmentId.Value),
                    cancellationToken);

    public Task<bool> HasLocalApplicationConflictAsync(
        int localAppId,
        DateTime dateTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    x.AppointmentDate == dateTime &&
                    x.Status != AppointmentStatus.Cancelled &&
                    x.Status != null &&
                    !x.IsLocked &&
                    (!excludeAppointmentId.HasValue ||
                     x.TestAppointmentID !=
                     excludeAppointmentId.Value),
                    cancellationToken);

    public Task<int?> GetApplicantPersonIdByLocalAppIdAsync(
        int localAppId,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0
            ? Task.FromResult<int?>(null)
            : _context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .Where(x => x.LocalDrivingLicenseApplicationID == localAppId)
                .Select(x => (int?)x.Application.ApplicantPersonID)
                .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsSlotOccupiedAsync(
        int testTypeId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default) =>
        testTypeId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TestTypeID == testTypeId &&
                    x.AppointmentDate == appointmentDate &&
                    x.Status != AppointmentStatus.Cancelled &&
                    x.Status != AppointmentStatus.NoShow &&
                    x.Status != null &&
                    (!excludeAppointmentId.HasValue ||
                     x.TestAppointmentID != excludeAppointmentId.Value),
                    cancellationToken);

    public Task<bool> HasApplicantSameDayConflictAsync(
        int applicantPersonId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (applicantPersonId <= 0)
            return Task.FromResult(false);

        var dayStart = appointmentDate.Date;
        var dayEnd = dayStart.AddDays(1);

        return _context.TestAppointments
            .AsNoTracking()
            .AnyAsync(x =>
                x.LocalDrivingLicenseApplication.Application.ApplicantPersonID == applicantPersonId &&
                x.AppointmentDate >= dayStart &&
                x.AppointmentDate < dayEnd &&
                x.Status != AppointmentStatus.Cancelled &&
                x.Status != null &&
                (!excludeAppointmentId.HasValue ||
                 x.TestAppointmentID != excludeAppointmentId.Value),
                cancellationToken);
    }

    public async Task<bool> AcquireSchedulingLocksAsync(
        int applicantPersonId,
        int testTypeId,
        DateTime appointmentDate,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsSqlServer())
            return true;

        var applicantResource = $"DVLD_Applicant_{applicantPersonId}_{appointmentDate:yyyyMMdd}";
        var slotResource = $"DVLD_Slot_{testTypeId}_{appointmentDate:yyyyMMddHHmm}";

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var currentTx = _context.Database.CurrentTransaction?.GetDbTransaction();

        var resources = new[] { applicantResource, slotResource };
        foreach (var resource in resources)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = currentTx;
            command.CommandText = "sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = Math.Max(30, (timeoutMs / 1000) + 5);

            var resParam = command.CreateParameter();
            resParam.ParameterName = "@Resource";
            resParam.Value = resource;
            command.Parameters.Add(resParam);

            var modeParam = command.CreateParameter();
            modeParam.ParameterName = "@LockMode";
            modeParam.Value = "Exclusive";
            command.Parameters.Add(modeParam);

            var ownerParam = command.CreateParameter();
            ownerParam.ParameterName = "@LockOwner";
            ownerParam.Value = "Transaction";
            command.Parameters.Add(ownerParam);

            var timeoutParam = command.CreateParameter();
            timeoutParam.ParameterName = "@LockTimeout";
            timeoutParam.Value = timeoutMs;
            command.Parameters.Add(timeoutParam);

            var returnParam = command.CreateParameter();
            returnParam.ParameterName = "@Result";
            returnParam.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnParam);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var resultValue = (int)(returnParam.Value ?? -999);
            if (resultValue < 0)
            {
                return false;
            }
        }

        return true;
    }

    public Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0 || testTypeId <= 0
            ? Task.FromResult(false)
            : _context.TestAppointments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId &&
                    x.TestTypeID == testTypeId &&
                    x.Status == AppointmentStatus.Scheduled &&
                    !x.IsLocked,
                    cancellationToken);

    public Task<AppStatus?> GetApplicationStatusAsync(
        int localAppId,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0
            ? Task.FromResult<AppStatus?>(null)
            : _context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID ==
                    localAppId)
                .Select(x =>
                    (AppStatus?)x.Application.ApplicationStatus)
                .FirstOrDefaultAsync(cancellationToken);

    public async Task<HashSet<int>> GetPassedTestTypeIdsAsync(
        int localAppId,
        CancellationToken cancellationToken = default)
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
                .ToListAsync(cancellationToken);

        return testTypeIds.ToHashSet();
    }

    public Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0 || testTypeId <= 0
            ? Task.FromResult(0)
            : _context.TestAppointments
                .AsNoTracking()
                .CountAsync(x =>
                    x.LocalDrivingLicenseApplicationID == localAppId &&
                    x.TestTypeID == testTypeId &&
                    (x.Status == AppointmentStatus.Completed ||
                     x.Status == AppointmentStatus.NoShow ||
                     (x.Status == null && x.Test != null)),
                    cancellationToken);

    public async Task AddAsync(
        TestAppointment appointment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        await _context.TestAppointments.AddAsync(
            appointment,
            cancellationToken);
    }

    public void Delete(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        _context.TestAppointments.Remove(appointment);
    }

    public async Task<bool> LockLocalApplicationForSchedulingAsync(
        int localAppId,
        CancellationToken cancellationToken = default)
    {
        if (localAppId <= 0)
            return false;

        var application = await _context.LocalDrivingLicenseApplications
            .FromSqlInterpolated($"""
            SELECT *
            FROM LocalDrivingLicenseApplications WITH (UPDLOCK, HOLDLOCK)
            WHERE LocalDrivingLicenseApplicationID = {localAppId}
            """)
            .FirstOrDefaultAsync(cancellationToken);

        return application is not null;
    }

    public async Task<(HashSet<DateTime> OccupiedSlots, HashSet<DateTime> ApplicantAppointmentDates, DateTime? LastTestDate)>
        GetAvailabilityDataAsync(
            int testTypeId,
            int applicantPersonId,
            int localAppId,
            DateTime rangeStart,
            DateTime rangeEnd,
            CancellationToken cancellationToken = default)
    {
        var occupiedSlots = (await _context.TestAppointments
            .AsNoTracking()
            .Where(x => x.TestTypeID == testTypeId &&
                        x.AppointmentDate >= rangeStart &&
                        x.AppointmentDate < rangeEnd &&
                        x.Status != AppointmentStatus.Cancelled &&
                        x.Status != AppointmentStatus.NoShow &&
                        x.Status != null)
            .Select(x => x.AppointmentDate)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var applicantAppointmentDates = (await _context.TestAppointments
            .AsNoTracking()
            .Where(x => x.LocalDrivingLicenseApplication.Application.ApplicantPersonID == applicantPersonId &&
                        x.AppointmentDate >= rangeStart &&
                        x.AppointmentDate < rangeEnd &&
                        x.Status != AppointmentStatus.Cancelled &&
                        x.Status != null)
            .Select(x => x.AppointmentDate.Date)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var lastTestDate = await _context.TestAppointments
            .AsNoTracking()
            .Where(x => x.LocalDrivingLicenseApplicationID == localAppId &&
                        (x.IsLocked || x.Test != null) &&
                        x.Status != AppointmentStatus.Cancelled &&
                        x.Status != null)
            .OrderByDescending(x => x.AppointmentDate)
            .Select(x => (DateTime?)x.AppointmentDate)
            .FirstOrDefaultAsync(cancellationToken);

        return (occupiedSlots, applicantAppointmentDates, lastTestDate);
    }

    public async Task<List<int>> GetPendingNoShowAppointmentIdsAsync(
        DateTime cutoffJordanTime,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            return [];

        return await _context.TestAppointments
            .AsNoTracking()
            .Where(x => x.Status == AppointmentStatus.Scheduled &&
                        !x.IsLocked &&
                        x.AppointmentDate <= cutoffJordanTime &&
                        !_context.Tests.Any(t => t.TestAppointmentID == x.TestAppointmentID))
            .OrderBy(x => x.AppointmentDate)
            .Take(batchSize)
            .Select(x => x.TestAppointmentID)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AcquireAppointmentLockAsync(
        int appointmentId,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsSqlServer() || appointmentId <= 0)
            return true;

        var resource = $"DVLD_Appointment_{appointmentId}";

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var currentTx = _context.Database.CurrentTransaction?.GetDbTransaction();

        await using var command = connection.CreateCommand();
        command.Transaction = currentTx;
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = Math.Max(30, (timeoutMs / 1000) + 5);

        var resParam = command.CreateParameter();
        resParam.ParameterName = "@Resource";
        resParam.Value = resource;
        command.Parameters.Add(resParam);

        var modeParam = command.CreateParameter();
        modeParam.ParameterName = "@LockMode";
        modeParam.Value = "Exclusive";
        command.Parameters.Add(modeParam);

        var ownerParam = command.CreateParameter();
        ownerParam.ParameterName = "@LockOwner";
        ownerParam.Value = "Transaction";
        command.Parameters.Add(ownerParam);

        var timeoutParam = command.CreateParameter();
        timeoutParam.ParameterName = "@LockTimeout";
        timeoutParam.Value = timeoutMs;
        command.Parameters.Add(timeoutParam);

        var returnParam = command.CreateParameter();
        returnParam.ParameterName = "@Result";
        returnParam.Direction = ParameterDirection.ReturnValue;
        command.Parameters.Add(returnParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var resultValue = (int)(returnParam.Value ?? -999);
        return resultValue >= 0;
    }

    public async Task<Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats> GetAttemptStatsAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        if (localAppId <= 0 || testTypeId <= 0)
        {
            return new Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats(localAppId, testTypeId, 0, 0, 0, 0, 1);
        }

        var appointments = await _context.TestAppointments
            .AsNoTracking()
            .Include(x => x.Test)
            .Where(x =>
                x.LocalDrivingLicenseApplicationID == localAppId &&
                x.TestTypeID == testTypeId)
            .ToListAsync(cancellationToken);

        var passed = 0;
        var failed = 0;
        var noShow = 0;

        foreach (var apt in appointments)
        {
            if (apt.Status == AppointmentStatus.Scheduled || apt.Status == AppointmentStatus.Cancelled)
            {
                continue;
            }

            if (apt.Test != null)
            {
                if (apt.Test.TestResult)
                    passed++;
                else
                    failed++;
            }
            else if (apt.Status == AppointmentStatus.NoShow)
            {
                noShow++;
            }
        }

        var total = passed + failed + noShow;
        var nextTrialNumber = total + 1;
        return new Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats(
            localAppId,
            testTypeId,
            total,
            passed,
            failed,
            noShow,
            nextTrialNumber);
    }

    public Task<List<TestAppointment>> GetActiveScheduledByLocalAppIdForUpdateAsync(
        int localAppId,
        CancellationToken cancellationToken = default) =>
        localAppId <= 0
            ? Task.FromResult<List<TestAppointment>>([])
            : _context.TestAppointments
                .Where(x =>
                    x.LocalDrivingLicenseApplicationID == localAppId &&
                    x.Status == AppointmentStatus.Scheduled &&
                    !x.IsLocked)
                .ToListAsync(cancellationToken);
}
