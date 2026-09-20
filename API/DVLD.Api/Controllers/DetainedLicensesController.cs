using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.DetainedLicense;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "StaffOnly")]
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
            return result.ToActionResult(this);

        if (result.Value is null)
            throw new InvalidOperationException(
                "Detained license service returned a successful result without data.");

        return Ok(
            result.Value
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        if (result.Value is null)
            throw new InvalidOperationException(
                "Detained license service returned a successful result without data.");

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpGet("license/{licenseId:int}/active")]
    public async Task<IActionResult> GetActiveByLicenseId(
        int licenseId)
    {
        var result =
            await service.GetActiveDetainByLicenseIdAsync(
                licenseId);

        if (result.IsFailure)
            return result.ToActionResult(this);

        if (result.Value is null)
            throw new InvalidOperationException(
                "Detained license service returned a successful result without data.");

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpGet("license/{licenseId:int}/detained")]
    public async Task<IActionResult> IsDetained(
        int licenseId)
    {
        var result =
            await service.IsLicenseDetainedAsync(
                licenseId);

        return Ok(
            new
            {
                detained = result
            });
    }

    [HttpPost]
    public async Task<IActionResult> Detain(
        [FromBody]
        CreateDetainedLicenseRequest request)
    {
        var result =
            await service.AddAsync(
                new CreateDetainedLicenseDto
                {
                    LicenseID = request.LicenseId,
                    FineFees = request.FineFees
                });

        if (result.IsFailure)
            return result.ToActionResult(this);

        if (result.Value is null)
            throw new InvalidOperationException(
                "Detained license service returned a successful result without data.");

        return Ok(
            MapToResponse(result.Value));
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(
        [FromBody]
        ReleaseDetainedLicenseRequest request)
    {
        var result =
            await service.ReleaseAsync(
                new ReleaseDetainedLicenseDto
                {
                    DetainID = request.DetainId
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private static DetainedLicenseResponse MapToResponse(
        DetainedLicenseDto dto)
        => new()
        {
            DetainId = dto.DetainID,
            LicenseId = dto.LicenseID,
            PersonId = dto.PersonID,
            NationalNo = dto.NationalNo,
            FullName = dto.FullName,
            DetainDate = dto.DetainDate,
            FineFees = dto.FineFees,
            CreatedByUserId = dto.CreatedByUserID,
            CreatedByUserName = dto.CreatedByUserName,
            IsReleased = dto.IsReleased,
            ReleaseDate = dto.ReleaseDate,
            ReleasedByUserId = dto.ReleasedByUserID,
            ReleaseApplicationId =
                dto.ReleaseApplicationID
        };
}