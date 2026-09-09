using Application.Interfaces;
using DVLD.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DashboardController(
    IDashboardService service) : ControllerBase
{
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result =
            await service.GetStatisticsAsync();

        var response = new DashboardResponse
        {
            TotalPeople = result.TotalPeople,
            TotalDrivers = result.TotalDrivers,
            ActiveLicenses = result.ActiveLicenses,
            PendingApplications = result.PendingApplications,
            LocalDrivingLicenseApplications =
                result.LocalDrivingLicenseApplications,
            InternationalLicenses =
                result.InternationalLicenses,
            DetainedLicenses =
                result.DetainedLicenses,
            UpcomingTests =
                result.UpcomingTests
        };

        return Ok(response);
    }
}