using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using DVLD.Api.Results;
using DVLD.Contracts.TestType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TestTypesController(
    ITestTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllTestTypesAsync();

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
            await service.GetTestTypeByIdAsync(id);

        if (result.IsFailure)
            return result.ToActionResult(this);

        return Ok(
            MapToResponse(result.Value!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTestTypeRequest request)
    {
        var result =
            await service.UpdateTestTypeAsync(
                id,
                new TestTypeDto
                {
                    TestTypeId = id,
                    TestTypeTitle =
                        request.TestTypeTitle,
                    TestTypeDescription =
                        request.TestTypeDescription,
                    TestTypeFees =
                        request.TestTypeFees
                });

        return result.IsSuccess
            ? NoContent()
            : result.ToActionResult(this);
    }

    private static TestTypeResponse MapToResponse(
        TestTypeDto dto)
        => new()
        {
            TestTypeId = dto.TestTypeId,
            TestTypeTitle = dto.TestTypeTitle,
            TestTypeDescription =
                dto.TestTypeDescription,
            TestTypeFees = dto.TestTypeFees
        };
}