using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.LicenseIssuance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class LicenseIssuanceController(
    ILicenseIssuanceService service) : ControllerBase
{
    [HttpPost("first-license")]
    public async Task<IActionResult> IssueFirstLicense(
        [FromBody] IssueFirstLicenseRequest request)
    {
        var result =
            await service.IssueFirstLicenseAsync(
                request.LocalApplicationId,
                request.Notes);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new IssueFirstLicenseResponse(
                result.Value));
    }
}