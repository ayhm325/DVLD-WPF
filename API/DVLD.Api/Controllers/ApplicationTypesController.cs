using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ApplicationTypesController : ControllerBase
{
    private readonly IApplicationTypeService _applicationTypeService;

    public ApplicationTypesController(
        IApplicationTypeService applicationTypeService)
    {
        _applicationTypeService = applicationTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _applicationTypeService
                .GetAllApplicationTypesAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(id);

        if (result.IsSuccess)
            return Ok(result.Value);

        return result.Error == "Application type not found."
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
    int id,
    ApplicationTypeDto dto)
    {
        var result =
            await _applicationTypeService
                .UpdateApplicationTypeAsync(id, dto);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new { error = result.Error });
    }


}