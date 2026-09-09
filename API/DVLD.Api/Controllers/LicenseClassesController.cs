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
public sealed class LicenseClassesController(
    ILicenseClassService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllLicenseClassesAsync();

        if (result.IsFailure)
            return HandleFailure(result);

        var response = result.Value?
            .Select(MapToResponse)
            .ToList() ?? [];

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetLicenseClassByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "License class data is unavailable." });
        }

        return Ok(MapToResponse(result.Value));
    }

    private static LicenseClassResponse MapToResponse(
        LicenseClassDto dto)
    {
        return new LicenseClassResponse
        {
            LicenseClassId = dto.LicenseClassID,
            LicenseClassName = dto.LicenseClassName,
            LicenseClassDescription = dto.LicenseClassDescription,
            MinAllowedAge = dto.MinAllowedAge,
            DefaultValidityLength = dto.DefaultValidityLength,
            LicenseClassFees = dto.LicenseClassFees
        };
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