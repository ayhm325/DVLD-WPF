using Application.DTOs;
using Application.Interfaces;
using DVLD.Api.Results;
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
            return result.ToActionResult(this);

        return Ok(
            result.Value?
                .Select(MapToResponse)
                .ToList()
            ?? []);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetLicenseClassByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        if (result.Value is null)
            throw new InvalidOperationException(
                "License class service returned a successful result without data.");

        return Ok(
            MapToResponse(result.Value));
    }

    private static LicenseClassResponse MapToResponse(
        LicenseClassDto dto)
        => new()
        {
            LicenseClassId =
                dto.LicenseClassID,

            LicenseClassName =
                dto.LicenseClassName,

            LicenseClassDescription =
                dto.LicenseClassDescription,

            MinAllowedAge =
                dto.MinAllowedAge,

            DefaultValidityLength =
                dto.DefaultValidityLength,

            LicenseClassFees =
                dto.LicenseClassFees
        };
}