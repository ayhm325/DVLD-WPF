using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DashboardRepository
    : IDashboardRepository
{
    private readonly DVLDDbContext _context;

    public DashboardRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // GET DASHBOARD STATISTICS
    // =========================================================

    public async Task<DashboardDto>
        GetStatisticsAsync()
    {
        var totalPeople =
            await _context.People
                .AsNoTracking()
                .CountAsync();

        var totalDrivers =
            await _context.Drivers
                .AsNoTracking()
                .CountAsync();

        var activeLicenses =
            await _context.Licenses
                .AsNoTracking()
                .CountAsync(
                    x => x.IsActive);

        var pendingApplications =
            await _context.Applications
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.ApplicationStatus ==
                        AppStatus.New);

        var localDrivingLicenseApplications =
            await _context
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .CountAsync();

        var internationalLicenses =
            await _context
                .InternationalLicenses
                .AsNoTracking()
                .CountAsync();

        var detainedLicenses =
            await _context
                .DetainedLicenses
                .AsNoTracking()
                .CountAsync(
                    x => !x.IsReleased);

        var upcomingTests =
            await _context
                .TestAppointments
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.AppointmentDate >=
                        DateTime.Today);

        return new DashboardDto
        {
            TotalPeople =
                totalPeople,

            TotalDrivers =
                totalDrivers,

            ActiveLicenses =
                activeLicenses,

            PendingApplications =
                pendingApplications,

            LocalDrivingLicenseApplications =
                localDrivingLicenseApplications,

            InternationalLicenses =
                internationalLicenses,

            DetainedLicenses =
                detainedLicenses,

            UpcomingTests =
                upcomingTests
        };
    }
}
