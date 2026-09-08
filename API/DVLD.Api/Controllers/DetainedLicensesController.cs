using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using DVLD.Contracts.DetainedLicense;
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

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    error =
                        "Detained license service returned no data."
                });
        }

        var response =
            result.Value
                .Select(MapToResponse)
                .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    error =
                        "Detained license service returned no data."
                });
        }

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpGet("license/{licenseId:int}/active")]
    public async Task<IActionResult> GetActiveByLicenseId(
        int licenseId)
    {
        var result =
            await service
                .GetActiveDetainByLicenseIdAsync(
                    licenseId);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    error =
                        "Detained license service returned no data."
                });
        }

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpGet("license/{licenseId:int}/detained")]
    public async Task<IActionResult> IsDetained(
        int licenseId)
    {
        var result =
            await service
                .IsLicenseDetainedAsync(
                    licenseId);

        return Ok(new
        {
            detained = result
        });
    }

    [HttpPost]
    public async Task<IActionResult> Detain(
        [FromBody] CreateDetainedLicenseRequest request)
    {
        var dto =
            new CreateDetainedLicenseDto
            {
                LicenseID =
                    request.LicenseId,

                FineFees =
                    request.FineFees
            };

        var result =
            await service.AddAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        if (result.Value is null)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    error =
                        "Detained license service returned no data."
                });
        }

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(
        [FromBody] ReleaseDetainedLicenseRequest request)
    {
        var dto =
            new ReleaseDetainedLicenseDto
            {
                DetainID =
                    request.DetainId
            };

        var result =
            await service.ReleaseAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    private static DetainedLicenseResponse MapToResponse(
        DetainedLicenseDto dto)
    {
        return new DetainedLicenseResponse
        {
            DetainId =
                dto.DetainID,

            LicenseId =
                dto.LicenseID,

            PersonId =
                dto.PersonID,

            NationalNo =
                dto.NationalNo,

            FullName =
                dto.FullName,

            DetainDate =
                dto.DetainDate,

            FineFees =
                dto.FineFees,

            CreatedByUserId =
                dto.CreatedByUserID,

            CreatedByUserName =
                dto.CreatedByUserName,

            IsReleased =
                dto.IsReleased,

            ReleaseDate =
                dto.ReleaseDate,

            ReleasedByUserId =
                dto.ReleasedByUserID,

            ReleaseApplicationId =
                dto.ReleaseApplicationID
        };
    }

    private static IActionResult HandleFailure(
        Result result)
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