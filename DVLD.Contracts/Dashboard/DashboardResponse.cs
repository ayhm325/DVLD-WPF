namespace DVLD.Contracts.Dashboard;

public sealed class DashboardResponse
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