using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class TestRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public TestRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================
        private IQueryable<Test> Query(DVLDDbContext context)
        {
            

            return context.Tests
                .AsNoTracking()
                .Include(t => t.TestAppointment)
                    .ThenInclude(a => a.TestType)
                .Include(t => t.TestAppointment)
                    .ThenInclude(a => a.LocalDrivingLicenseApplication)
                .Include(t => t.User);
        }

        // =========================
        // GET
        // =========================

        public async Task<Test?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(t => t.TestID == id);
        }

        public async Task<List<Test>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<List<Test>> GetByTestAppointmentIdAsync(int appointmentId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(t => t.TestAppointmentID == appointmentId)
                .ToListAsync();
        }

        public async Task<List<Test>> GetByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(t => t.CreatedByUserID == userId)
                .ToListAsync();
        }

        public async Task<int> GetTrialCountByApplicationIdAsync(int ldlAppId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tests
                .Where(t => t.TestAppointment != null &&
                            t.TestAppointment.LocalDrivingLicenseApplicationID == ldlAppId)
                .CountAsync();
        }

        // =========================
        // CHECKS
        // =========================

        public async Task<bool> IsTestExistsAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tests
                .AnyAsync(t => t.TestID == id);
        }

        public async Task<bool> IsTestAlreadyTakenAsync(int appointmentId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tests
                .AnyAsync(t => t.TestAppointmentID == appointmentId);
        }

        // =========================
        // CREATE
        // =========================

        public async Task<int> AddAsync(Test test)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.Tests.AddAsync(test);
            await context.SaveChangesAsync();

            return test.TestID;
        }

        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateAsync(Test test)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.Tests
                .FirstOrDefaultAsync(t => t.TestID == test.TestID);

            if (existing is null)
                return false;

            context.Entry(existing)
                .CurrentValues
                .SetValues(test);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var entity = await context.Tests.FindAsync(id);

            if (entity is null)
                return false;

            context.Tests.Remove(entity);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // ADDITIONAL OPERATIONS
        // =========================
        public async Task<int> CountAsync(Expression<Func<Test, bool>> predicate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tests.CountAsync(predicate);
        }
    }
}