using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.LicenseRenewal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
[Route("api/[controller]")]
public sealed class LicenseRenewalController(
    ILicenseRenewalService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Renew(
        [FromBody] RenewLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var result =
            await service.RenewLicenseAsync(
                request.OldLicenseId,
                request.Notes,
                cancellationToken);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            new RenewLicenseResponse
            {
                LicenseId = result.Value
            });
    }
}