using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DashboardRepository
    : IDashboardRepository
{
    private readonly IDbContextFactory<DVLDDbContext>
        _contextFactory;


    public DashboardRepository(
        IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory =
            contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }


    // =========================================================
    // GET DASHBOARD STATISTICS
    // =========================================================

    public async Task<DashboardDto>
        GetStatisticsAsync()
    {
        await using var context =
            await _contextFactory
                .CreateDbContextAsync();


        // =====================================================
        // PEOPLE
        // =====================================================

        var totalPeople =
            await context.People
                .AsNoTracking()
                .CountAsync();


        // =====================================================
        // DRIVERS
        // =====================================================

        var totalDrivers =
            await context.Drivers
                .AsNoTracking()
                .CountAsync();


        // =====================================================
        // ACTIVE LICENSES
        // =====================================================

        var activeLicenses =
            await context.Licenses
                .AsNoTracking()
                .CountAsync(
                    x => x.IsActive);


        // =====================================================
        // PENDING APPLICATIONS
        // =====================================================
        //
        // New       = Pending
        // Completed = Not Pending
        // Cancelled = Not Pending
        //
        // =====================================================

        var pendingApplications =
            await context.Applications
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.ApplicationStatus ==
                        AppStatus.New);


        // =====================================================
        // LOCAL DRIVING LICENSE APPLICATIONS
        // =====================================================

        var localDrivingLicenseApplications =
            await context
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .CountAsync();


        // =====================================================
        // INTERNATIONAL LICENSES
        // =====================================================

        var internationalLicenses =
            await context
                .InternationalLicenses
                .AsNoTracking()
                .CountAsync();


        // =====================================================
        // DETAINED LICENSES
        // =====================================================
        //
        // IsReleased = false
        // means the license is currently detained.
        //
        // =====================================================

        var detainedLicenses =
            await context
                .DetainedLicenses
                .AsNoTracking()
                .CountAsync(
                    x => !x.IsReleased);


        // =====================================================
        // UPCOMING TESTS
        // =====================================================
        //
        // Today and future appointments.
        //
        // =====================================================

        var upcomingTests =
            await context
                .TestAppointments
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.AppointmentDate >=
                        DateTime.Today);


        // =====================================================
        // RETURN DASHBOARD DTO
        // =====================================================

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