using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using DVLD.Contracts.ApplicationType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ApplicationTypesController(
    IApplicationTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllApplicationTypesAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        var response = result.Value!
            .Select(MapToResponse)
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetApplicationTypeByIdAsync(id);

        if (result.IsSuccess)
            return Ok(MapToResponse(result.Value!));

        return result.ErrorType switch
        {
            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            _ =>
                StatusCode(500, new { error = result.Error })
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateApplicationTypeRequest request)
    {
        var dto = new ApplicationTypeDto
        {
            ApplicationTypeId = id,
            ApplicationTypeTitle = request.ApplicationTypeTitle,
            ApplicationTypeFees = request.ApplicationTypeFees
        };

        var result =
            await service.UpdateApplicationTypeAsync(id, dto);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Conflict =>
                Conflict(new { error = result.Error }),

            ErrorType.Forbidden =>
                Forbid(),

            _ =>
                StatusCode(500, new { error = result.Error })
        };
    }

    private static ApplicationTypeResponse MapToResponse(
        ApplicationTypeDto dto)
    {
        return new ApplicationTypeResponse
        {
            ApplicationTypeId = dto.ApplicationTypeId,
            ApplicationTypeTitle = dto.ApplicationTypeTitle,
            ApplicationTypeFees = dto.ApplicationTypeFees
        };
    }
}