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

        return Ok(result);
    }
}