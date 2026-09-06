using Application.Common.Results;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LicenseRenewalController(
    ILicenseRenewalService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Renew(
        [FromBody] RenewLicenseRequest request)
    {
        var result =
            await service.RenewLicenseAsync(
                request.OldLicenseId,
                request.Notes);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(new { licenseId = result.Value });
    }

    private static IActionResult HandleFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new { error = result.Error }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new { error = result.Error }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new { error = result.Error }),

            ErrorType.Forbidden =>
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(new { error = result.Error })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
        };
    }
}

public sealed record RenewLicenseRequest(
    int OldLicenseId,
    string? Notes);