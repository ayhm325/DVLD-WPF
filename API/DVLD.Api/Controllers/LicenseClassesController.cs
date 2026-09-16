using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using DVLD.Contracts.LicenseClass;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class LicenseClassesController(ILicenseClassService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllLicenseClassesAsync();
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value?.Select(MapToResponse).ToList() ?? []);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetLicenseClassByIdAsync(id);
        if (result.IsFailure) return HandleFailure(result);

        return result.Value is null
            ? ServerError("License class data is unavailable.")
            : Ok(MapToResponse(result.Value));
    }

    private static LicenseClassResponse MapToResponse(LicenseClassDto dto) => new()
    {
        LicenseClassId = dto.LicenseClassID,
        LicenseClassName = dto.LicenseClassName,
        LicenseClassDescription = dto.LicenseClassDescription,
        MinAllowedAge = dto.MinAllowedAge,
        DefaultValidityLength = dto.DefaultValidityLength,
        LicenseClassFees = dto.LicenseClassFees
    };

    private static IActionResult HandleFailure(Result result) =>
        result.ErrorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(
                new { error = result.Error }),

            ErrorType.NotFound => new NotFoundObjectResult(
                new { error = result.Error }),

            ErrorType.Forbidden => new ObjectResult(
                new { error = result.Error })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            _ => ServerError(result.Error)
        };

    private static IActionResult ServerError(string error) =>
        new ObjectResult(new { error })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
}