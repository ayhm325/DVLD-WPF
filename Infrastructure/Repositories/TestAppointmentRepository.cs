using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class TestAppointmentRepository : ITestAppointmentRepository
{
    private readonly DVLDDbContext _context;

    public TestAppointmentRepository(DVLDDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // ===== BASE QUERY =====

    private IQueryable<TestAppointment> Query() => _context.TestAppointments
        .Include(x => x.TestType)
        .Include(x => x.LocalDrivingLicenseApplication)
        .Include(x => x.User)
        .Include(x => x.Test)
        .Include(x => x.RetakeTestApplication);

    // ===== GET =====

    public async Task<TestAppointment?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        return await Query().AsNoTracking().FirstOrDefaultAsync(x => x.TestAppointmentID == id);
    }

    public async Task<List<TestAppointment>> GetAllAsync() =>
        await Query().AsNoTracking().ToListAsync();

    public async Task<List<TestAppointment>> GetByLocalDrivingLicenseApplicationIdAsync(int localDrivingLicenseApplicationId)
    {
        if (localDrivingLicenseApplicationId <= 0) return [];
        return await Query().AsNoTracking()
            .Where(x => x.LocalDrivingLicenseApplicationID == localDrivingLicenseApplicationId)
            .ToListAsync();
    }

    public async Task<List<TestAppointment>> GetByTestTypeIdAsync(TestTypeEnum testType)
    {
        if (!Enum.IsDefined(testType)) return [];
        return await Query().AsNoTracking()
            .Where(x => x.TestTypeID == (int)testType)
            .ToListAsync();
    }

    public async Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId)
    {
        if (userId <= 0) return [];
        return await Query().AsNoTracking()
            .Where(x => x.CreatedByUserID == userId)
            .ToListAsync();
    }

    public async Task<TestAppointment?> GetScheduleInfoAsync(int testAppointmentId)
    {
        if (testAppointmentId <= 0) return null;

        return await _context.TestAppointments.AsNoTracking()
            .Include(x => x.TestType)
            .Include(x => x.LocalDrivingLicenseApplication).ThenInclude(x => x.Application).ThenInclude(a => a.Person)
            .Include(x => x.LocalDrivingLicenseApplication).ThenInclude(x => x.Application).ThenInclude(a => a.ApplicationType)
            .Include(x => x.LocalDrivingLicenseApplication).ThenInclude(x => x.LicenseClass)
            .FirstOrDefaultAsync(x => x.TestAppointmentID == testAppointmentId);
    }

    // ===== CHECKS =====

    public async Task<bool> ExistsAsync(Expression<Func<TestAppointment, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await _context.TestAppointments.AsNoTracking().AnyAsync(predicate);
    }

    public async Task<bool> HasConflictAsync(int localAppId, int testTypeId, DateTime dateTime, int? excludeAppointmentId = null)
    {
        if (localAppId <= 0 || testTypeId <= 0) return false;

        return await _context.TestAppointments.AsNoTracking()
            .AnyAsync(x =>
                x.LocalDrivingLicenseApplicationID == localAppId &&
                x.TestTypeID == testTypeId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue || x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public async Task<bool> HasUserConflictAsync(int userId, DateTime dateTime, int? excludeAppointmentId = null)
    {
        if (userId <= 0) return false;

        return await _context.TestAppointments.AsNoTracking()
            .AnyAsync(x =>
                x.CreatedByUserID == userId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue || x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public async Task<bool> HasLocalApplicationConflictAsync(int localAppId, DateTime dateTime, int? excludeAppointmentId = null)
    {
        if (localAppId <= 0) return false;

        return await _context.TestAppointments.AsNoTracking()
            .AnyAsync(x =>
                x.LocalDrivingLicenseApplicationID == localAppId &&
                x.AppointmentDate == dateTime &&
                !x.IsLocked &&
                (!excludeAppointmentId.HasValue || x.TestAppointmentID != excludeAppointmentId.Value));
    }

    public async Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId)
    {
        if (localAppId <= 0 || testTypeId <= 0) return false;

        // Check for pending (unlocked) appointments
        var hasPending = await _context.TestAppointments.AsNoTracking()
            .AnyAsync(a => a.LocalDrivingLicenseApplicationID == localAppId && a.TestTypeID == testTypeId && !a.IsLocked);

        if (hasPending) return true;

        // Check if already passed
        return await _context.Tests.AsNoTracking()
            .AnyAsync(t => t.TestAppointment != null &&
                           t.TestAppointment.LocalDrivingLicenseApplicationID == localAppId &&
                           t.TestAppointment.TestTypeID == testTypeId &&
                           t.TestResult);
    }

    public async Task<AppStatus?> GetApplicationStatusAsync(int localAppId)
    {
        if (localAppId <= 0) return null;

        return await _context.LocalDrivingLicenseApplications
            .Where(x => x.LocalDrivingLicenseApplicationID == localAppId)
            .Select(x => (AppStatus?)x.Application.ApplicationStatus)
            .FirstOrDefaultAsync();
    }

    // ===== COMMANDS =====

    public async Task<bool> AddAsync(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        await _context.TestAppointments.AddAsync(appointment);
        return true; // Save handled by UnitOfWork
    }

    public async Task<bool> UpdateAsync(TestAppointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        if (appointment.TestAppointmentID <= 0) return false;

        var existing = await _context.TestAppointments.FirstOrDefaultAsync(x => x.TestAppointmentID == appointment.TestAppointmentID);
        if (existing is null) return false;

        existing.TestTypeID = appointment.TestTypeID;
        existing.LocalDrivingLicenseApplicationID = appointment.LocalDrivingLicenseApplicationID;
        existing.AppointmentDate = appointment.AppointmentDate;
        existing.PaidFees = appointment.PaidFees;       
        existing.IsLocked = appointment.IsLocked;
        existing.RetakeTestApplicationID = appointment.RetakeTestApplicationID;

        return true; // Save handled by UnitOfWork
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0) return;

        var entity = await _context.TestAppointments.FirstOrDefaultAsync(x => x.TestAppointmentID == id);
        if (entity is null) return;

        _context.TestAppointments.Remove(entity);
        // Save handled by UnitOfWork
    }
}