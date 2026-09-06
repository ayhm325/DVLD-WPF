using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DetainedLicensesController(
    IDetainedLicenseService service) : ControllerBase
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

    [HttpGet("license/{licenseId:int}/active")]
    public async Task<IActionResult> GetActiveByLicenseId(
        int licenseId)
    {
        var result =
            await service.GetActiveDetainByLicenseIdAsync(
                licenseId);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("license/{licenseId:int}/detained")]
    public async Task<IActionResult> IsDetained(
        int licenseId)
    {
        var result =
            await service.IsLicenseDetainedAsync(licenseId);

        return Ok(new { detained = result });
    }

    [HttpPost]
    public async Task<IActionResult> Detain(
        [FromBody] CreateDetainedLicenseDto dto)
    {
        var result =
            await service.AddAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(
        [FromBody] ReleaseDetainedLicenseDto dto)
    {
        var result =
            await service.ReleaseAsync(dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
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