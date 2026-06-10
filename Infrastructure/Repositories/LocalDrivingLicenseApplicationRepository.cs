using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class LocalDrivingLicenseApplicationRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public LocalDrivingLicenseApplicationRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        
        public async Task<List<LocalDrivingLicenseApplication>> GetAllAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application)
                .ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                .ToListAsync();
        }

        // 
        public async Task<List<LocalDrivingLicenseApplication>> GetLocalApplicationsOnlyAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application)
                    .ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                .Where(a => a.Application.ApplicationTypeID == 1) // Local License فقط
                .ToListAsync();
        }

        public async Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application)
                .ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                .FirstOrDefaultAsync(t => t.LocalDrivingLicenseApplicationID == id);
        }

        public async Task<int> GetPassedTestCountAsync(int localAppId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tests
                .CountAsync(t => t.TestAppointment != null &&
                                 t.TestAppointment.LocalDrivingLicenseApplicationID == localAppId &&
                                 t.TestResult == 1);
        }

        public async Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application).ThenInclude(a => a.Person)
                .Include(a => a.LicenseClass)
                .Where(a => a.Application.ApplicantPersonID == personId)
                .ToListAsync();
        }

        public async Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(int applicationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application).ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                .Where(a => a.ApplicationID == applicationId)
                .ToListAsync();
        }

        public async Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(int licenseClassId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.LocalDrivingLicenseApplications
                .Include(a => a.Application).ThenInclude(app => app.Person)
                .Include(a => a.LicenseClass)
                .Where(a => a.LicenseClassID == licenseClassId)
                .ToListAsync();
        }

        // =========================
        // CREATE
        // =========================
        public async Task<int> CreatLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplication LDLApp)
        {
            using var context = await _contextFactory.CreateDbContextAsync();            
            await context.LocalDrivingLicenseApplications.AddAsync(LDLApp);
            await context.SaveChangesAsync();
            return LDLApp.LocalDrivingLicenseApplicationID;
        }
        //// =========================
        //// UPDATE
        //// =========================
        public async Task<bool> UpdateAsync(LocalDrivingLicenseApplication entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.LocalDrivingLicenseApplications.FindAsync(entity.LocalDrivingLicenseApplicationID);
            if (existing == null) return false;
            context.Entry(existing).CurrentValues.SetValues(entity);
            return await context.SaveChangesAsync() > 0;
        }


        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeleteAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.LocalDrivingLicenseApplications.FindAsync(id);
            if (existing == null) return false;
            context.LocalDrivingLicenseApplications.Remove(existing);
            return await context.SaveChangesAsync() > 0;
        }
    
        
    }
}