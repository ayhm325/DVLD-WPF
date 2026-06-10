
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;

namespace Infrastructure.Repositories
{
    public class ApplicationRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public ApplicationRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<ApplicationD?> GetApplicationByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.ApplicationID == id);
        }

        public async Task<List<ApplicationD>> GetAllApplicationsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .ToListAsync();
        }

        public async Task<List<ApplicationD>> GetApplicationsByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .Where(a => a.ApplicantPersonID == personId)
                .ToListAsync();
        }

        public async Task<List<ApplicationD>> GetApplicationsByApplicationTypeIdAsync(int applicationTypeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .Where(a => a.ApplicationTypeID == applicationTypeId)
                .ToListAsync();
        }

        public async Task<List<ApplicationD>> GetApplicationsByUserIdAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .Where(a => a.CreatedByUserID == userId)
                .ToListAsync();
        }

        public async Task<List<ApplicationD>> GetApplicationsByStausAsync(int status)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser)
                .Where(a => a.ApplicationStatus == status)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================
        public async Task<bool> IsApplicationExistsByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.ApplicationID == id);
        }

        public async Task<bool> IsPersonHasActiveApplicationAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.ApplicantPersonID == personId && a.ApplicationStatus == (byte)AppStatus.New);
        }

        public async Task<bool> IsPersonHasActiveApplicationOfTypeAsync(int personId, int applicationTypeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.ApplicantPersonID == personId && a.ApplicationTypeID == applicationTypeId && a.ApplicationStatus == (byte)AppStatus.New);
        }

        public async Task<bool> IsUserHasActiveApplicationAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.CreatedByUserID == userId && a.ApplicationStatus == (byte)AppStatus.Completed);
        }

        public async Task<bool> IsUserHasActiveApplicationOfTypeAsync(int userId, int applicationTypeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.CreatedByUserID == userId && a.ApplicationTypeID == applicationTypeId && a.ApplicationStatus == (byte)AppStatus.Completed);
        }

        public async Task<bool> IsApplicationTypeHasActiveApplicationsAsync(int applicationTypeId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Applications.AnyAsync(a => a.ApplicationTypeID == applicationTypeId && a.ApplicationStatus == (byte)AppStatus.Completed);
        }

        public bool IsApplicationStatusValid(int status)
        {
            return status == (byte)AppStatus.New ||
                   status == (byte)AppStatus.Cancelled ||
                   status == (byte)AppStatus.Completed;             
        }

        public async Task<int?> HasDuplicateApplicationAsync(int personId, int licenseClassId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var app = await context.LocalDrivingLicenseApplications
                .Where(ldla =>
                        ldla.Application.ApplicantPersonID == personId &&
                        ldla.LicenseClassID == licenseClassId &&
                        (
                            ldla.Application.ApplicationStatus == (byte)AppStatus.New ||
                            ldla.Application.ApplicationStatus == (byte)AppStatus.Completed
                         ))
                .Select(ldla => ldla.ApplicationID)
                .FirstOrDefaultAsync();

            return app == 0 ? null : app;
        }

        // =========================
        // CREATE
        // =========================
        public async Task<int> CreateApplicationAsync(ApplicationD application)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.Applications.AddAsync(application);
            await context.SaveChangesAsync();
            return application.ApplicationID;
        }

        //// =========================
        //// UPDATE
        //// =========================
        public async Task<bool> UpdateApplicationAsync(ApplicationD application)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Applications.FindAsync(application.ApplicationID);
            if (existing is null) return false;

            context.Entry(existing).CurrentValues.SetValues(application);
            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeleteApplicationAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Applications.FindAsync(id);
            if (existing is null) return false;

            context.Applications.Remove(existing);
            return await context.SaveChangesAsync() > 0;
        }

        // End of class
    }
}
