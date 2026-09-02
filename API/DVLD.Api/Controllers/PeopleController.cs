using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly IPersonService _personService;

    public PeopleController(IPersonService personService)
    {
        _personService = personService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _personService.GetAllPeopleAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _personService.GetPersonByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpGet("national/{nationalNo}")]
    public async Task<IActionResult> GetByNationalNo(string nationalNo)
    {
        var result =
            await _personService.GetPersonByNationalNoAsync(nationalNo);

        return result.IsSuccess
            ? Ok(result.Value)
            : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PersonCreateDto dto)
    {
        var result =
            await _personService.AddPersonAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new { personId = result.Value });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        PersonUpdateDto dto)
    {
        var result =
            await _personService.UpdatePersonAsync(id, dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _personService.DeletePersonAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private IActionResult HandleFailure(Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                BadRequest(new { error = result.Error }),

            ErrorType.NotFound =>
                NotFound(new { error = result.Error }),

            ErrorType.Conflict =>
                Conflict(new { error = result.Error }),

            _ =>
                StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = result.Error })
        };
    }
}
