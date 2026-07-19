namespace Application.DTOs;

public class DashboardDto
{
    public int TotalPeople { get; set; }
    public int TotalDrivers { get; set; }
    public int ActiveLicenses { get; set; }
    public int PendingApplications { get; set; }

    public int LocalDrivingLicenseApplications { get; set; }
    public int InternationalLicenses { get; set; }
    public int DetainedLicenses { get; set; }
    public int UpcomingTests { get; set; }
}