using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class TestAppointmentRepository : ITestAppointmentRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public TestAppointmentRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY (DRY)
        // =========================
        private IQueryable<TestAppointment> Query(DVLDDbContext context)
        {
            return context.TestAppointments
                .Include(x => x.TestType)
                .Include(x => x.LocalDrivingLicenseApplication)
                .Include(x => x.User)
                .Include(x => x.Test)
                .Include(x => x.RetakeTestApplication);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<TestAppointment?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(x => x.TestAppointmentID == id);
        }

        public async Task<List<TestAppointment>> GetAllAsync()
        {
            await  using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByApplicationIdAsync(int applicationId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.LocalDrivingLicenseApplicationID == applicationId)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByTestTypeIdAsync(TestTypeEnum testType)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.TestTypeID == (int)testType)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.CreatedByUserID == userId)
                .ToListAsync();
        }

        public async Task<TestAppointment?> GetScheduleInfoAsync(int testAppointmentId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .Include(x => x.TestType)

                .Include(x => x.LocalDrivingLicenseApplication)
                    .ThenInclude(x => x.Application)
                        .ThenInclude(a => a.Person)

                .Include(x => x.LocalDrivingLicenseApplication)
                    .ThenInclude(x => x.Application)
                        .ThenInclude(a => a.ApplicationType)

                .Include(x => x.LocalDrivingLicenseApplication)
                    .ThenInclude(x => x.LicenseClass)

                .FirstOrDefaultAsync(x =>
                    x.TestAppointmentID == testAppointmentId);
        }

        // =========================
        // CHECK OPERATIONS
        // =========================

        public async Task<bool> ExistsAsync(Expression<Func<TestAppointment, bool>> predicate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AsNoTracking()
                .AnyAsync(predicate);
        }

        public async Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.TestTypeID == testTypeId &&
                    x.AppointmentDate == dateTime);
        }

        public async Task<bool> HasUserConflictAsync(int userId, DateTime dateTime)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.CreatedByUserID == userId &&
                    x.AppointmentDate == dateTime);
        }

        public async Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID == applicationId &&
                    x.AppointmentDate == dateTime);
        }

        public async Task<bool> HasPassedAllTestsAsync(int appId)
        {
            await  using var context = await _contextFactory.CreateDbContextAsync();

            var passedTests = await context.Tests
                .Where(t =>
                    t.TestAppointment != null &&
                    t.TestAppointment.LocalDrivingLicenseApplication != null &&
                    t.TestAppointment.LocalDrivingLicenseApplication.ApplicationID == appId &&
                    t.TestResult)
                .Select(t => t.TestAppointment!.TestTypeID)
                .Distinct()
                .CountAsync();

            return passedTests == Enum.GetValues<TestTypeEnum>().Length;
        }

        public async Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // 1. التحقق من وجود موعد "مفتوح" (غير مقفل) لنفس التطبيق ونفس نوع الاختبار
            bool hasPendingAppointment = await context.TestAppointments
                .AnyAsync(a => a.LocalDrivingLicenseApplicationID == localAppId
                            && a.TestTypeID == testTypeId
                            && a.IsLocked ); // == false 

            if (hasPendingAppointment) return true;

            // 2. التحقق من وجود "نجاح" سابق لنفس نوع الاختبار
            // إذا وجد سجل ناجح (TestResult == true)، نمنع إنشاء موعد جديد
            bool hasPassed = await context.Tests
                .AnyAsync(t =>
                    t.TestAppointment != null &&
                    t.TestAppointment.LocalDrivingLicenseApplicationID == localAppId &&
                    t.TestAppointment.TestTypeID == testTypeId &&
                    t.TestResult);

            return hasPassed;
        }

        // =========================
        // CREATE
        // =========================

        public async Task<bool> AddAsync(TestAppointment appointment)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            await context.TestAppointments.AddAsync(appointment);
            return await context.SaveChangesAsync()>0;
        }

        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateAsync(TestAppointment appointment)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.TestAppointments
                .FirstOrDefaultAsync(x => x.TestAppointmentID == appointment.TestAppointmentID);

            if (existing is null)
                throw new InvalidOperationException($"Test appointment with ID {appointment.TestAppointmentID} was not found.");

            existing.TestTypeID = appointment.TestTypeID;
            existing.LocalDrivingLicenseApplicationID = appointment.LocalDrivingLicenseApplicationID;
            existing.AppointmentDate = appointment.AppointmentDate;
            existing.PaidFees = appointment.PaidFees;
            existing.CreatedByUserID = appointment.CreatedByUserID;
            existing.IsLocked = appointment.IsLocked;
            existing.RetakeTestApplicationID = appointment.RetakeTestApplicationID;

            await context.SaveChangesAsync();
            return true;
        }

        // =========================
        // DELETE
        // =========================

        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.TestAppointments.FindAsync(id);

            if (entity is null)
                return;

            context.TestAppointments.Remove(entity);
            await context.SaveChangesAsync();
        }

       
    }
}