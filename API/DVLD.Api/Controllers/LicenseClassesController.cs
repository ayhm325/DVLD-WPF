using Application.Common.Results;
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
public sealed class LicenseClassesController(ILicenseClassService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllLicenseClassesAsync();
        if (result.IsFailure) return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult()
            : Ok(result.Value.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetLicenseClassByIdAsync(id);
        if (result.IsFailure) return result.ToActionResult(this);

        return result.Value is null
            ? UnexpectedResult()
            : Ok(MapToResponse(result.Value));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateLicenseClassRequest request)
    {
        var dto = new LicenseClassDto
        {
            LicenseClassID = id,
            LicenseClassName = request.LicenseClassName,
            LicenseClassDescription = request.LicenseClassDescription,
            MinAllowedAge = request.MinAllowedAge,
            DefaultValidityLength = request.DefaultValidityLength,
            LicenseClassFees = request.LicenseClassFees
        };

        var result = await service.UpdateLicenseClassAsync(id, dto);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private IActionResult UnexpectedResult() =>
        Result.Failure("License class service returned a successful result without data.")
            .ToActionResult(this);

    private static LicenseClassResponse MapToResponse(LicenseClassDto dto) => new()
    {
        LicenseClassId = dto.LicenseClassID,
        LicenseClassName = dto.LicenseClassName,
        LicenseClassDescription = dto.LicenseClassDescription,
        MinAllowedAge = dto.MinAllowedAge,
        DefaultValidityLength = dto.DefaultValidityLength,
        LicenseClassFees = dto.LicenseClassFees
    };
}
