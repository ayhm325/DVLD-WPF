using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
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
        var result = await service.GetAllApplicationTypesAsync();
        if (result.IsFailure) return HandleFailure(result);

        return Ok(result.Value!.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetApplicationTypeByIdAsync(id);
        if (result.IsFailure) return HandleFailure(result);

        return Ok(MapToResponse(result.Value!));
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

        var result = await service.UpdateApplicationTypeAsync(id, dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static ApplicationTypeResponse MapToResponse(
        ApplicationTypeDto dto) => new()
        {
            ApplicationTypeId = dto.ApplicationTypeId,
            ApplicationTypeTitle = dto.ApplicationTypeTitle,
            ApplicationTypeFees = dto.ApplicationTypeFees
        };

    private static IActionResult HandleFailure(Result result) =>
        result.ErrorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(
                new { error = result.Error }),

            ErrorType.NotFound => new NotFoundObjectResult(
                new { error = result.Error }),

            ErrorType.Conflict => new ConflictObjectResult(
                new { error = result.Error }),

            ErrorType.Forbidden => new ObjectResult(
                new { error = result.Error })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            _ => new ObjectResult(
                new { error = result.Error })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}