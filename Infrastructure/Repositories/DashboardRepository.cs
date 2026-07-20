using Application.DTOs;
using Application.Interfaces;
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


        public async Task<DashboardDto> GetStatisticsAsync()
        {
            await using var context =
                await _factory.CreateDbContextAsync();


            return new DashboardDto
            {
                TotalPeople =
                    await context.People.CountAsync(),


                TotalDrivers =
                    await context.Drivers.CountAsync(),


                ActiveLicenses =
                    await context.Licenses
                    .CountAsync(x => x.IsActive),


                PendingApplications =
                    await context.Applications
                    .CountAsync(x => x.ApplicationStatus != 3),


                LocalDrivingLicenseApplications =
                    await context.LocalDrivingLicenseApplications
                    .CountAsync(),


                InternationalLicenses =
                    await context.InternationalLicenses
                    .CountAsync(),


                DetainedLicenses =
                    await context.DetainedLicenses
                    .CountAsync(x => !x.IsReleased),


                UpcomingTests =
                    await context.TestAppointments
                    .CountAsync(x =>
                        x.AppointmentDate >= DateTime.Today)
            };
        }
    }
}