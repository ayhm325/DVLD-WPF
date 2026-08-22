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
        _personRepository = personRepository
            ?? throw new ArgumentNullException(nameof(personRepository));
    }

    // =========================
    // GET ALL
    // =========================

    public async Task<Result<List<PersonDto>>> GetAllPeopleAsync()
    {
        var people = await _personRepository.GetAllPersonsAsync();

        var peopleDtos = people
            .Select(MapToDto)
            .ToList();

        return Result<List<PersonDto>>.Success(peopleDtos);
    }

    // =========================
    // GET BY ID
    // =========================

    public async Task<Result<PersonDto>> GetPersonByIdAsync(int id)
    {
        if (id <= 0)
            return Result<PersonDto>.FromFailure("Invalid person ID.");

        var person = await _personRepository.GetPersonByIdAsync(id);

        if (person is null)
            return Result<PersonDto>.FromFailure("Person not found.");

        return Result<PersonDto>.Success(MapToDto(person));
    }

    // =========================
    // GET BY NATIONAL NUMBER
    // =========================

    public async Task<Result<PersonDto>> GetPersonByNationalNoAsync(
        string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
            return Result<PersonDto>.FromFailure(
                "National number is required.");

        var person = await _personRepository.GetPersonByNationalNoAsync(
            nationalNo.Trim());

        if (person is null)
            return Result<PersonDto>.FromFailure("Person not found.");

        return Result<PersonDto>.Success(MapToDto(person));
    }

    // =========================
    // EXISTS
    // =========================

    public async Task<bool> IsPersonExistsAsync(int id)
    {
        if (id <= 0)
            return false;

        return await _personRepository.IsPersonExistsByIdAsync(id);
    }

    // =========================
    // CREATE
    // =========================

    public async Task<Result<int>> AddPersonAsync(
        PersonCreateUpdateDto dto)
    {
        if (dto is null)
            return Result<int>.FromFailure(
                "Person data is required.");

        // 1. Validate DTO
        var validation = PersonValidator.Validate(dto);

        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        // 2. Map DTO -> Entity
        var person = MapToEntity(dto);

        // 3. Business rule: National Number must be unique
        var isDuplicated =
            await _personRepository.IsNationalNoDuplicatedAsync(
                person.NationalNo,
                0);

        if (isDuplicated)
        {
            return Result<int>.FromFailure(
                "The national number is already registered.");
        }

        // 4. Persist
        var personId =
            await _personRepository.AddPersonAsync(person);

        return Result<int>.Success(personId);
    }

    // =========================
    // UPDATE
    // =========================

    public async Task<Result> UpdatePersonAsync(
        int id,
        PersonCreateUpdateDto dto)
    {
        if (id <= 0)
            return Result.Failure(
                "Invalid person ID.");

        if (dto is null)
            return Result.Failure(
                "Person data is required.");

        // 1. Check existence
        var exists =
            await _personRepository.IsPersonExistsByIdAsync(id);

        if (!exists)
            return Result.Failure(
                "Person not found.");

        // 2. Validate DTO
        var validation = PersonValidator.Validate(dto);

        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        // 3. Map DTO -> Entity
        var person = MapToEntity(dto);

        // Make sure the ID comes from the method argument
        person.PersonId = id;

        // 4. Business rule: National Number must be unique
        var isDuplicated =
            await _personRepository.IsNationalNoDuplicatedAsync(
                person.NationalNo,
                id);

        if (isDuplicated)
        {
            return Result.Failure(
                "The national number is already registered to another person.");
        }

        // 5. Persist
        var success =
            await _personRepository.UpdatePersonAsync(person);

        return success
            ? Result.Success()
            : Result.Failure("Failed to update person.");
    }

    // =========================
    // DELETE
    // =========================

    public async Task<Result> DeletePersonAsync(int id)
    {
        if (id <= 0)
            return Result.Failure(
                "Invalid person ID.");

        // Check existence
        var exists =
            await _personRepository.IsPersonExistsByIdAsync(id);

        if (!exists)
            return Result.Failure(
                "Person not found.");

        // Persist
        var success =
            await _personRepository.DeletePersonAsync(id);

        return success
            ? Result.Success()
            : Result.Failure("Failed to delete person.");
    }

    // =========================
    // ENTITY -> DTO
    // =========================

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

    // =========================
    // DTO -> ENTITY
    // =========================

    private static Person MapToEntity(
        PersonCreateUpdateDto dto)
    {
        return new Person
        {
            PersonId = dto.PersonId,

            NationalNo =
                dto.NationalNo?.Trim() ?? string.Empty,

            FirstName =
                dto.FirstName?.Trim() ?? string.Empty,

            SecondName =
                dto.SecondName?.Trim() ?? string.Empty,

            ThirdName =
                string.IsNullOrWhiteSpace(dto.ThirdName)
                    ? null
                    : dto.ThirdName.Trim(),

            LastName =
                dto.LastName?.Trim() ?? string.Empty,

            DateOfBirth = dto.DateOfBirth,

            Gender = dto.Gender,

            Address =
                dto.Address?.Trim() ?? string.Empty,

            Phone =
                dto.Phone?.Trim() ?? string.Empty,

            Email =
                string.IsNullOrWhiteSpace(dto.Email)
                    ? null
                    : dto.Email.Trim(),

            NationalityCountryID =
                dto.NationalityCountryID,

            ImagePath =
                string.IsNullOrWhiteSpace(dto.ImagePath)
                    ? null
                    : dto.ImagePath.Trim()
        };
    }
}