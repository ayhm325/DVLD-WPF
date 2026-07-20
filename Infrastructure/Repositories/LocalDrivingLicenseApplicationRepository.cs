using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LocalDrivingLicenseApplicationRepository : ILocalDrivingLicenseApplicationRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public LocalDrivingLicenseApplicationRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================
        private IQueryable<LocalDrivingLicenseApplication> Query(DVLDDbContext context)
        {
            return context.LocalDrivingLicenseApplications
                .AsNoTracking()
                .Include(a => a.Application)
                    .ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<List<LocalDrivingLicenseApplication>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        public async Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // سنقوم بجلب الكائن مع التأكد من إدراج الخصائص المطلوبة
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application)
                    .ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                // إضافة هذه السطور لضمان عدم ضياع القيمة أثناء الـ Mapping
                .AsTracking() // إزالة AsNoTracking في حال كنت تريد تعديل البيانات أو ضمان دقة القراءة
                .FirstOrDefaultAsync(t => t.LocalDrivingLicenseApplicationID == id);
        }
        //public async Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id)
        //{
        //    using var context = await _contextFactory.CreateDbContextAsync();

        //    return await Query(context)
        //        .FirstOrDefaultAsync(t =>
        //            t.LocalDrivingLicenseApplicationID == id);
        //}

        public async Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(a => a.Application.ApplicantPersonID == personId)
                .ToListAsync();
        }

        public async Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(a => a.ApplicationID == applicationId)
                .ToListAsync();
        }

        public async Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(int licenseClassId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .Where(a => a.LicenseClassID == licenseClassId)
                .ToListAsync();
        }

        // =========================
        // EXTRA QUERY (NO BASE QUERY NEEDED)
        // =========================

        public async Task<int> GetPassedTestCountAsync(int localAppId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Tests
                .CountAsync(t =>
                    t.TestAppointment != null &&
                    t.TestAppointment.LocalDrivingLicenseApplicationID == localAppId &&
                    t.TestResult == true);
        }

        public async Task<int?> GetApplicationIdByLocalIdAsync(int localId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.LocalDrivingLicenseApplications
                .Where(x => x.LocalDrivingLicenseApplicationID == localId)
                .Select(x => x.ApplicationID)
                .FirstOrDefaultAsync();
        }

        // =========================
        // CREATE
        // =========================

        public async Task<int> CreateLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplication LDLApp)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            await context.LocalDrivingLicenseApplications.AddAsync(LDLApp);
            await context.SaveChangesAsync();

            return LDLApp.LocalDrivingLicenseApplicationID;
        }

        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateAsync(LocalDrivingLicenseApplication entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.LocalDrivingLicenseApplications
                .FindAsync(entity.LocalDrivingLicenseApplicationID);

            if (existing == null)
                return false;

            context.Entry(existing)
                .CurrentValues
                .SetValues(entity);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.LocalDrivingLicenseApplications
                .FindAsync(id);

            if (existing == null)
                return false;

            context.LocalDrivingLicenseApplications.Remove(existing);

            return await context.SaveChangesAsync() > 0;
        }


        



    }
}