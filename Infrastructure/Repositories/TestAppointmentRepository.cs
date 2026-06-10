using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class TestAppointmentRepository
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
                .Include(x => x.RetakeTestApplication);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<TestAppointment?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(x => x.TestAppointmentID == id);
        }

        public async Task<List<TestAppointment>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByApplicationIdAsync(int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.LocalDrivingLicenseApplicationID == applicationId)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByTestTypeIdAsync(int testTypeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.TestTypeID == testTypeId)
                .ToListAsync();
        }

        public async Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(x => x.CreatedByUserID == userId)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================

        public async Task<bool> ExistsAsync(Expression<Func<TestAppointment, bool>> predicate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AsNoTracking()
                .AnyAsync(predicate);
        }

        public async Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.TestTypeID == testTypeId &&
                    x.AppointmentDate == dateTime);
        }

        public async Task<bool> HasUserConflictAsync(int userId, DateTime dateTime)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.CreatedByUserID == userId &&
                    x.AppointmentDate == dateTime);
        }

        public async Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestAppointments
                .AnyAsync(x =>
                    x.LocalDrivingLicenseApplicationID == applicationId &&
                    x.AppointmentDate == dateTime);
        }

        // =========================
        // CREATE
        // =========================

        public async Task AddAsync(TestAppointment appointment)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.TestAppointments.AddAsync(appointment);
            await context.SaveChangesAsync();
        }

        // =========================
        // UPDATE
        // =========================

        public async Task UpdateAsync(TestAppointment appointment)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.TestAppointments
                .FirstOrDefaultAsync(x => x.TestAppointmentID == appointment.TestAppointmentID);

            if (existing is null)
                throw new InvalidOperationException("Test appointment not found.");

            context.Entry(existing).CurrentValues.SetValues(appointment);

            await context.SaveChangesAsync();
        }

        // =========================
        // DELETE
        // =========================

        public async Task DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.TestAppointments.FindAsync(id);

            if (entity is null)
                return;

            context.TestAppointments.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}