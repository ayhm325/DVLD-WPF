using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;


        public ApplicationRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }


        #region Query

        private IQueryable<ApplicationD> Query(
            DVLDDbContext context)
        {
            return context.Applications
                .AsNoTracking()
                .Include(a => a.Person)
                .Include(a => a.ApplicationType)
                .Include(a => a.CreatedByUser);
        }

        #endregion



        #region Get


        public async Task<ApplicationD?> GetApplicationByIdAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationID == id);
        }



        public async Task<List<ApplicationD>> GetAllApplicationsAsync()
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .ToListAsync();
        }



        public async Task<List<ApplicationD>> GetApplicationsByPersonIdAsync(
            int personId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .Where(a =>
                    a.ApplicantPersonID == personId)
                .ToListAsync();
        }



        public async Task<List<ApplicationD>> GetApplicationsByApplicationTypeIdAsync(
            int applicationTypeId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .Where(a =>
                    a.ApplicationTypeID == applicationTypeId)
                .ToListAsync();
        }



        public async Task<List<ApplicationD>> GetApplicationsByUserIdAsync(
            int userId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .Where(a =>
                    a.CreatedByUserID == userId)
                .ToListAsync();
        }



        public async Task<List<ApplicationD>> GetApplicationsByStatusAsync(
            int status)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await Query(context)
                .Where(a =>
                    a.ApplicationStatus == status)
                .ToListAsync();
        }


        #endregion




        #region Validation Queries


        public async Task<bool> IsApplicationExistsByIdAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Applications
                .AnyAsync(a =>
                    a.ApplicationID == id);
        }



        public async Task<bool> IsPersonHasActiveApplicationAsync(
            int personId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Applications
                .AnyAsync(a =>
                    a.ApplicantPersonID == personId &&
                    a.ApplicationStatus ==
                    (byte)AppStatus.New);
        }



        public async Task<bool> IsPersonHasActiveApplicationOfTypeAsync(
            int personId,
            int applicationTypeId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            return await context.Applications
                .AnyAsync(a =>
                    a.ApplicantPersonID == personId &&
                    a.ApplicationTypeID == applicationTypeId &&
                    a.ApplicationStatus ==
                    (byte)AppStatus.New);
        }



        public async Task<int?> HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            var applicationId =
                await context.LocalDrivingLicenseApplications
                .Where(ldla =>
                    ldla.Application.ApplicantPersonID == personId &&
                    ldla.LicenseClassID == licenseClassId &&
                    (
                    ldla.Application.ApplicationStatus ==
                    (byte)AppStatus.New

                    ||

                    ldla.Application.ApplicationStatus ==
                    (byte)AppStatus.Completed
                    ))
                .Select(ldla =>
                    ldla.ApplicationID)
                .FirstOrDefaultAsync();



            return applicationId == 0
                ? null
                : applicationId;
        }


        #endregion




        #region Create


        public async Task<int> AddNewApplicationAsync(
            ApplicationD application)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            await context.Applications.AddAsync(application);


            await context.SaveChangesAsync();


            return application.ApplicationID;
        }


        #endregion




        #region Update


        public async Task<bool> UpdateApplicationAsync(
            ApplicationD application)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            var existing =
                await context.Applications
                .FindAsync(application.ApplicationID);



            if (existing == null)
                return false;



            context.Entry(existing)
                .CurrentValues
                .SetValues(application);



            return await context.SaveChangesAsync() > 0;
        }



        #endregion




        #region Delete


        public async Task<bool> DeleteApplicationAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();


            var application =
                await context.Applications
                .FindAsync(id);



            if (application == null)
                return false;



            context.Applications.Remove(application);



            return await context.SaveChangesAsync() > 0;
        }


        #endregion




        #region Status Operations


        public async Task<bool> CompleteApplicationAsync(
            int applicationId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();



            var application =
                await context.Applications
                .FindAsync(applicationId);



            if (application == null)
                return false;



            application.ApplicationStatus =
                (byte)AppStatus.Completed;


            application.LastStatusDate =
                DateTime.UtcNow;



            return await context.SaveChangesAsync() > 0;
        }




        public async Task<bool> CancelApplicationAsync(
            int applicationId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();



            var application =
                await context.Applications
                .FindAsync(applicationId);



            if (application == null)
                return false;



            application.ApplicationStatus =
                (byte)AppStatus.Cancelled;


            application.LastStatusDate =
                DateTime.UtcNow;



            return await context.SaveChangesAsync() > 0;
        }


        #endregion
    }
}