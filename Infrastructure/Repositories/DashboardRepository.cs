using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DashboardRepository(DVLDDbContext context)
    : IDashboardRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    public async Task<DashboardDto> GetStatisticsAsync()
    {
        var sql = """
            SELECT
                (SELECT COUNT(*) FROM People) AS TotalPeople,
                (SELECT COUNT(*) FROM Drivers) AS TotalDrivers,
                (SELECT COUNT(*) FROM Licenses WHERE IsActive = 1) AS ActiveLicenses,
                (SELECT COUNT(*) FROM Applications WHERE ApplicationStatus = 1) AS PendingApplications,
                (SELECT COUNT(*) FROM LocalDrivingLicenseApplications) AS LocalDrivingLicenseApplications,
                (SELECT COUNT(*) FROM InternationalLicenses) AS InternationalLicenses,
                (SELECT COUNT(*) FROM DetainedLicenses WHERE IsReleased = 0) AS DetainedLicenses,
                (SELECT COUNT(*) FROM TestAppointments WHERE AppointmentDate >= CAST(GETDATE() AS date)) AS UpcomingTests
            """;

        var result = await _context.Database
            .SqlQueryRaw<DashboardStatisticsRow>(sql)
            .SingleAsync();

        return new DashboardDto
        {
            TotalPeople = result.TotalPeople,
            TotalDrivers = result.TotalDrivers,
            ActiveLicenses = result.ActiveLicenses,
            PendingApplications = result.PendingApplications,
            LocalDrivingLicenseApplications =
                result.LocalDrivingLicenseApplications,
            InternationalLicenses = result.InternationalLicenses,
            DetainedLicenses = result.DetainedLicenses,
            UpcomingTests = result.UpcomingTests
        };
    }

    private sealed class DashboardStatisticsRow
    {
        public int TotalPeople { get; init; }
        public int TotalDrivers { get; init; }
        public int ActiveLicenses { get; init; }
        public int PendingApplications { get; init; }
        public int LocalDrivingLicenseApplications { get; init; }
        public int InternationalLicenses { get; init; }
        public int DetainedLicenses { get; init; }
        public int UpcomingTests { get; init; }
    }
}