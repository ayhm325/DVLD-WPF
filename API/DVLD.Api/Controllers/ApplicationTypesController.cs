using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
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

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetApplicationTypeByIdAsync(id);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(new { error = result.Error }),
            ErrorType.Validation => BadRequest(new { error = result.Error }),
            _ => StatusCode(500, new { error = result.Error })
        };
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ApplicationTypeDto dto)
    {
        var result = await service.UpdateApplicationTypeAsync(id, dto);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(new { error = result.Error }),
            ErrorType.NotFound => NotFound(new { error = result.Error }),
            ErrorType.Conflict => Conflict(new { error = result.Error }),
            ErrorType.Forbidden => Forbid(),
            _ => StatusCode(500, new { error = result.Error })
        };
    }
}