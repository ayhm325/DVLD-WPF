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

        public async Task<LocalDrivingLicenseApplication> AddFullApplicationAsync(int personId, int licenseClassId, int createdByUserId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            // Create the parent application.
            var newApplication = new ApplicationD
            {
                ApplicantPersonID = personId,
                ApplicationDate = DateTime.Now,
                ApplicationTypeID = 1,
                ApplicationStatus = 1,
                LastStatusDate = DateTime.Now,
                CreatedByUserID = createdByUserId
            };
            context.Applications.Add(newApplication);
            // Save to generate ApplicationID.
            await context.SaveChangesAsync();
            // Create the local license application.
            var localApp = new LocalDrivingLicenseApplication
            {
                ApplicationID = newApplication.ApplicationID,
                LicenseClassID = licenseClassId
            };
            context.LocalDrivingLicenseApplications.Add(localApp);
            await context.SaveChangesAsync();
            return localApp;
        }

        public async Task<bool> UpdateAsync(LocalDrivingLicenseApplication entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.LocalDrivingLicenseApplications.Update(entity);
            return await context.SaveChangesAsync() > 0;
        }

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