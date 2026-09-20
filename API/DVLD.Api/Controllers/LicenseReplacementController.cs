using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.LicenseReplacement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class LicenseReplacementController(
    ILicenseReplacementService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Replace(
        [FromBody]
        ReplaceLicenseRequest request)
    {
        var result =
            await service.ReplaceLicenseAsync(
                request.OldLicenseId,
                request.ReplacementReason);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new ReplaceLicenseResponse
            {
                LicenseId = result.Value
            });
    }
}