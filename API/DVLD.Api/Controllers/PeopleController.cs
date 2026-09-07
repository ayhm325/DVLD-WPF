using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using ContractPerson = DVLD.Contracts.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PeopleController(
    IPersonService personService)
    : ControllerBase
{
    private readonly IPersonService _personService =
        personService
        ?? throw new ArgumentNullException(nameof(personService));

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _personService.GetAllPeopleAsync();

        if (result.IsFailure)
            return HandleFailure(result);

        var response =
            result.Value!
                .Select(ToResponse)
                .ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result =
            await _personService.GetPersonByIdAsync(id);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpGet("national/{nationalNo}")]
    public async Task<IActionResult> GetByNationalNo(
        string nationalNo)
    {
        var result =
            await _personService
                .GetPersonByNationalNoAsync(nationalNo);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ContractPerson.CreatePersonRequest request)
    {
        var dto =
            new PersonCreateDto
            {
                NationalNo = request.NationalNo,
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ThirdName = request.ThirdName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = (Domain.Enums.Gender)request.Gender,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                NationalityCountryID =
                    request.NationalityCountryID,
                ImagePath = request.ImagePath
            };

        var result =
            await _personService.AddPersonAsync(dto);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value },
            new ContractPerson.CreatePersonResponse
            {
                PersonId = result.Value
            });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ContractPerson.UpdatePersonRequest request)
    {
        var dto =
            new PersonUpdateDto
            {
                NationalNo = request.NationalNo,
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ThirdName = request.ThirdName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth,
                Gender = (Domain.Enums.Gender)request.Gender,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                NationalityCountryID =
                    request.NationalityCountryID,
                ImagePath = request.ImagePath
            };

        var result =
            await _personService
                .UpdatePersonAsync(id, dto);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result =
            await _personService
                .DeletePersonAsync(id);

        return result.IsSuccess
            ? NoContent()
            : HandleFailure(result);
    }

    private static ContractPerson.PersonResponse ToResponse(
        PersonDto dto)
    {
        return new ContractPerson.PersonResponse
        {
            PersonId = dto.PersonId,
            NationalNo = dto.NationalNo,
            FirstName = dto.FirstName,
            SecondName = dto.SecondName,
            ThirdName = dto.ThirdName,
            LastName = dto.LastName,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = (ContractPerson.Gender)dto.Gender,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            NationalityCountryID =
                dto.NationalityCountryID,
            CountryName = dto.CountryName,
            ImagePath = dto.ImagePath
        };
    }

    private static IActionResult HandleFailure(
        Result result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.NotFound =>
                new NotFoundObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.Conflict =>
                new ConflictObjectResult(
                    new
                    {
                        error = result.Error
                    }),

            ErrorType.Forbidden =>
                new ObjectResult(
                    new
                    {
                        error = result.Error
                    })
                {
                    StatusCode =
                        StatusCodes.Status403Forbidden
                },

            _ =>
                new ObjectResult(
                    new
                    {
                        error = result.Error
                    })
                {
                    StatusCode =
                        StatusCodes.Status500InternalServerError
                }
        };
    }
}