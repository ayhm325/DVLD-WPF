
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _factory;

        public DashboardRepository(
            IDbContextFactory<DVLDDbContext> factory)
        {
            _factory = factory
                ?? throw new ArgumentNullException(nameof(factory));
        }


        // =========================================================
        // GET DASHBOARD STATISTICS
        // =========================================================

        public async Task<DashboardDto> GetStatisticsAsync()
        {
            await using var context =
                await _factory.CreateDbContextAsync();


            return new DashboardDto
            {
                // -------------------------------------------------
                // PEOPLE
                // -------------------------------------------------

                TotalPeople =
                    await context.People
                        .CountAsync(),


                // -------------------------------------------------
                // DRIVERS
                // -------------------------------------------------

                TotalDrivers =
                    await context.Drivers
                        .CountAsync(),


                // -------------------------------------------------
                // ACTIVE LICENSES
                // -------------------------------------------------

                ActiveLicenses =
                    await context.Licenses
                        .CountAsync(x => x.IsActive),


                // -------------------------------------------------
                // PENDING APPLICATIONS
                // -------------------------------------------------
                // Pending means New only.
                //
                // New        = Pending
                // Cancelled  = Not Pending
                // Completed  = Not Pending
                // -------------------------------------------------

                PendingApplications =
                    await context.Applications
                        .CountAsync(x =>
                            x.ApplicationStatus ==
                            AppStatus.New),


                // -------------------------------------------------
                // LOCAL DRIVING LICENSE APPLICATIONS
                // -------------------------------------------------

                LocalDrivingLicenseApplications =
                    await context.LocalDrivingLicenseApplications
                        .CountAsync(),


                // -------------------------------------------------
                // INTERNATIONAL LICENSES
                // -------------------------------------------------

                InternationalLicenses =
                    await context.InternationalLicenses
                        .CountAsync(),


                // -------------------------------------------------
                // DETAINED LICENSES
                // -------------------------------------------------

                DetainedLicenses =
                    await context.DetainedLicenses
                        .CountAsync(x => !x.IsReleased),


                // -------------------------------------------------
                // UPCOMING TESTS
                // -------------------------------------------------

                UpcomingTests =
                    await context.TestAppointments
                        .CountAsync(x =>
                            x.AppointmentDate >= DateTime.Today)
            };
        }
    }
}
