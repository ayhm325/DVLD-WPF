using Application.Common.Results;
using Application.Interfaces;
using DVLD.Contracts.LicenseReplacement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LicenseReplacementController(
    ILicenseReplacementService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Replace(
        [FromBody] ReplaceLicenseRequest request)
    {
        var result =
            await service.ReplaceLicenseAsync(
                request.OldLicenseId,
                request.ReplacementReason);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(
            new ReplaceLicenseResponse
            {
                LicenseId = result.Value
            });
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
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new { error = result.Error })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}