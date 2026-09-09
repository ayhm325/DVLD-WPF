using Application.Common.Results;
using Application.Interfaces;
using DVLD.Contracts.TestType;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class TestTypesController(
    ITestTypeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await service.GetAllTestTypesAsync();

        if (result.IsFailure)
            return HandleFailure(result);

        var response =
            result.Value!
                .Select(MapToResponse)
                .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await service.GetTestTypeByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(MapToResponse(result.Value!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdateTestTypeRequest request)
    {
        var dto = new Application.DTOs.TestTypeDTO.TestTypeDto
        {
            TestTypeId = id,
            TestTypeTitle = request.TestTypeTitle,
            TestTypeDescription = request.TestTypeDescription,
            TestTypeFees = request.TestTypeFees
        };

        var result =
            await service.UpdateTestTypeAsync(
                id,
                dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static TestTypeResponse MapToResponse(
        Application.DTOs.TestTypeDTO.TestTypeDto dto)
    {
        return new TestTypeResponse
        {
            TestTypeId = dto.TestTypeId,
            TestTypeTitle = dto.TestTypeTitle,
            TestTypeDescription = dto.TestTypeDescription,
            TestTypeFees = dto.TestTypeFees
        };
    }

    private IActionResult HandleFailure<T>(
        Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound =>
                NotFound(new
                {
                    message = result.Error
                }),

            ErrorType.Conflict =>
                Conflict(new
                {
                    message = result.Error
                }),

            ErrorType.Validation =>
                BadRequest(new
                {
                    message = result.Error
                }),

            _ =>
                BadRequest(new
                {
                    message = result.Error
                })
        };
    }

    private IActionResult HandleFailure(
        Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound =>
                NotFound(new
                {
                    message = result.Error
                }),

            ErrorType.Conflict =>
                Conflict(new
                {
                    message = result.Error
                }),

            ErrorType.Validation =>
                BadRequest(new
                {
                    message = result.Error
                }),

            _ =>
                BadRequest(new
                {
                    message = result.Error
                })
        };
    }
}