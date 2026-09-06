using Application.Common.Results;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class InternationalLicensesController(
    IInternationalService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("driver/{driverId:int}")]
    public async Task<IActionResult> GetByDriverId(
        int driverId)
    {
        var result =
            await service.GetByDriverIdAsync(driverId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("application/{applicationId:int}")]
    public async Task<IActionResult> GetByApplicationId(
        int applicationId)
    {
        var result =
            await service.GetByApplicationIdAsync(applicationId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("local-license/{localLicenseId:int}")]
    public async Task<IActionResult> GetByLocalLicenseId(
        int localLicenseId)
    {
        var result =
            await service.GetByLocalLicenseIdAsync(localLicenseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("license/{licenseId:int}/info")]
    public async Task<IActionResult> GetLocalLicenseInfo(
        int licenseId)
    {
        var result =
            await service.GetLocalLicenseInfoAsync(licenseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Issue(
        [FromBody] IssueInternationalLicenseRequest request)
    {
        var result =
            await service.IssueInternationalLicenseAsync(
                request.LocalLicenseId);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(new { internationalLicenseId = result.Value });
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

public sealed record IssueInternationalLicenseRequest(
    int LocalLicenseId);