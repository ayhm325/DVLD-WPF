using Application.DTOs;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.ApplicationType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public sealed class ApplicationTypesController(
    IApplicationTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllApplicationTypesAsync();

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            result.Value!
                .Select(MapToResponse)
                .ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetApplicationTypeByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody]
        UpdateApplicationTypeRequest request)
    {
        var dto = new ApplicationTypeDto
        {
            ApplicationTypeId = id,
            ApplicationTypeTitle =
                request.ApplicationTypeTitle,
            ApplicationTypeFees =
                request.ApplicationTypeFees
        };

        var result =
            await service.UpdateApplicationTypeAsync(
                id,
                dto);

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private static ApplicationTypeResponse MapToResponse(
        ApplicationTypeDto dto)
        => new()
        {
            ApplicationTypeId =
                dto.ApplicationTypeId,

            ApplicationTypeTitle =
                dto.ApplicationTypeTitle,

            ApplicationTypeFees =
                dto.ApplicationTypeFees
        };
}