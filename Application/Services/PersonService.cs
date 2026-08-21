using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
    }

    // GET ALL
    public async Task<Result<List<PersonDto>>> GetAllPeopleAsync()
    {
        var people = await _personRepository.GetAllPersonsAsync();
        return Result<List<PersonDto>>.Success(people.Select(MapToDto).ToList());
    }

    // GET BY ID
    public async Task<Result<PersonDto>> GetPersonByIdAsync(int id)
    {
        if (id <= 0)
            return Result<PersonDto>.FromFailure("Invalid person ID.");

        var person = await _personRepository.GetPersonByIdAsync(id);
        if (person is null)
            return Result<PersonDto>.FromFailure("Person not found.");

        return Result<PersonDto>.Success(MapToDto(person));
    }

    // GET BY NATIONAL NUMBER
    public async Task<Result<PersonDto>> GetPersonByNationalNoAsync(string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
            return Result<PersonDto>.FromFailure("National number is required.");

        var person = await _personRepository.GetPersonByNationalNoAsync(nationalNo.Trim());
        if (person is null)
            return Result<PersonDto>.FromFailure("Person not found.");

        return Result<PersonDto>.Success(MapToDto(person));
    }

    // EXISTS
    public async Task<bool> IsPersonExistsAsync(int id)
    {
        if (id <= 0) return false;
        return await _personRepository.IsPersonExistsByIdAsync(id);
    }

    // ADD
    public async Task<Result<int>> AddPersonAsync(PersonCreateUpdateDto dto)
    {
        if (dto is null)
            return Result<int>.FromFailure("Person data is required.");

        var person = MapToEntity(dto);
        var validation = PersonValidator.Validate(person);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        // National number must be unique
        if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, 0))
            return Result<int>.FromFailure("The national number is already registered.");

        var personId = await _personRepository.AddPersonAsync(person);
        return Result<int>.Success(personId);
    }

    // UPDATE
    public async Task<Result> UpdatePersonAsync(int id, PersonCreateUpdateDto dto)
    {
        if (id <= 0)
            return Result.Failure("Invalid person ID.");

        if (dto is null)
            return Result.Failure("Person data is required.");

        if (!await _personRepository.IsPersonExistsByIdAsync(id))
            return Result.Failure("Person not found.");

        var person = MapToEntity(dto);
        person.PersonId = id;

        var validation = PersonValidator.Validate(person);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        // National number must be unique
        if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, id))
            return Result.Failure("The national number is already registered to another person.");

        var success = await _personRepository.UpdatePersonAsync(person);
        return success ? Result.Success() : Result.Failure("Failed to update person.");
    }

    // DELETE
    public async Task<Result> DeletePersonAsync(int id)
    {
        if (id <= 0)
            return Result.Failure("Invalid person ID.");

        if (!await _personRepository.IsPersonExistsByIdAsync(id))
            return Result.Failure("Person not found.");

        var success = await _personRepository.DeletePersonAsync(id);
        return success ? Result.Success() : Result.Failure("Failed to delete person.");
    }

    // ENTITY -> DTO
    private static PersonDto MapToDto(Person person)
    {
        return new PersonDto
        {
            PersonId = person.PersonId,
            NationalNo = person.NationalNo,
            FullName = person.FullName,
            DateOfBirth = person.DateOfBirth,
            Gender = person.Gender,
            Address = person.Address,
            Phone = person.Phone,
            Email = person.Email,
            CountryName = person.Country?.CountryName ?? "Unknown",
            NationalityCountryID = person.NationalityCountryID,
            ImagePath = person.ImagePath
        };
    }

    // DTO -> ENTITY
    private static Person MapToEntity(PersonCreateUpdateDto dto)
    {
        return new Person
        {
            PersonId = dto.PersonId,
            NationalNo = dto.NationalNo?.Trim() ?? string.Empty,
            FirstName = dto.FirstName?.Trim() ?? string.Empty,
            SecondName = dto.SecondName?.Trim() ?? string.Empty,
            ThirdName = string.IsNullOrWhiteSpace(dto.ThirdName) ? null : dto.ThirdName.Trim(),
            LastName = dto.LastName?.Trim() ?? string.Empty,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Address = dto.Address?.Trim() ?? string.Empty,
            Phone = dto.Phone?.Trim() ?? string.Empty,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            NationalityCountryID = dto.NationalityCountryID,
            ImagePath = string.IsNullOrWhiteSpace(dto.ImagePath) ? null : dto.ImagePath.Trim()
        };
    }
}