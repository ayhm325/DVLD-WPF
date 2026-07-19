using Application.DTOs;
using Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<DVLDDbContext> _factory;

    public DashboardService(
        IDbContextFactory<DVLDDbContext> factory)
    {
        _factory = factory;
    }


    public async Task<DashboardDto> GetStatisticsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();


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
                await context.LocalDrivingLicenseApplications.CountAsync(),


            InternationalLicenses =
                await context.InternationalLicenses.CountAsync(),


            DetainedLicenses =
                await context.DetainedLicenses
                .CountAsync(x => !x.IsReleased),


            UpcomingTests =
                await context.TestAppointments
                .CountAsync(x => x.AppointmentDate >= DateTime.Today)
        };
    }
}