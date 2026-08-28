
using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;

    public PersonService(
        IPersonRepository personRepository)
    {
        _personRepository =
            personRepository
            ?? throw new ArgumentNullException(
                nameof(personRepository));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<PersonDto>>>
        GetAllPeopleAsync()
    {
        var people =
            await _personRepository
                .GetAllPersonsAsync();

        var result =
            people
                .Select(PersonMapper.ToDto)
                .ToList();

        return Result<List<PersonDto>>
            .Success(result);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<PersonDto>>
        GetPersonByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<PersonDto>
                .FromValidationFailure(
                    "Invalid person ID.");
        }

        var person =
            await _personRepository
                .GetPersonByIdAsync(id);

        if (person is null)
        {
            return Result<PersonDto>
                .FromNotFound(
                    "Person not found.");
        }

        return Result<PersonDto>
            .Success(
                PersonMapper.ToDto(person));
    }

    // =========================================================
    // GET BY NATIONAL NUMBER
    // =========================================================

    public async Task<Result<PersonDto>>
        GetPersonByNationalNoAsync(
            string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
        {
            return Result<PersonDto>
                .FromValidationFailure(
                    "National number is required.");
        }

        var normalizedNationalNo =
            nationalNo.Trim();

        if (normalizedNationalNo.Length != 10 ||
            normalizedNationalNo.Any(
                c => !char.IsDigit(c)))
        {
            return Result<PersonDto>
                .FromValidationFailure(
                    "National number must be exactly 10 digits.");
        }

        var person =
            await _personRepository
                .GetPersonByNationalNoAsync(
                    normalizedNationalNo);

        if (person is null)
        {
            return Result<PersonDto>
                .FromNotFound(
                    "Person not found.");
        }

        return Result<PersonDto>
            .Success(
                PersonMapper.ToDto(person));
    }

    // =========================================================
    // EXISTS
    // =========================================================

    public async Task<bool>
        IsPersonExistsAsync(int id)
    {
        if (id <= 0)
            return false;

        return await _personRepository
            .IsPersonExistsByIdAsync(id);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>>
        AddPersonAsync(
            PersonCreateDto personDto)
    {
        if (personDto is null)
        {
            return Result<int>
                .FromValidationFailure(
                    "Person data is required.");
        }

        // -----------------------------------------------------
        // 1. Validate input
        // -----------------------------------------------------

        var validation =
            PersonValidator.Validate(personDto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // 2. Map DTO -> Entity
        // -----------------------------------------------------

        var person =
            PersonMapper.ToEntity(personDto);

        // -----------------------------------------------------
        // 3. Check NationalNo uniqueness
        // -----------------------------------------------------

        var duplicated =
            await _personRepository
                .IsNationalNoDuplicatedAsync(
                    person.NationalNo,
                    0);

        if (duplicated)
        {
            return Result<int>
                .FromConflict(
                    "The national number is already registered.");
        }

        // -----------------------------------------------------
        // 4. Persist
        // -----------------------------------------------------

        try
        {
            var personId =
                await _personRepository
                    .AddPersonAsync(person);

            return Result<int>
                .Success(personId);
        }
        catch (Exception)
        {
            // We will later replace this with a more specific
            // database exception handler for the unique index.
            throw;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdatePersonAsync(
            int id,
            PersonUpdateDto personDto)
    {
        // -----------------------------------------------------
        // 1. Validate ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid person ID.");
        }

        // -----------------------------------------------------
        // 2. Validate DTO
        // -----------------------------------------------------

        if (personDto is null)
        {
            return Result.ValidationFailure(
                "Person data is required.");
        }

        var validation =
            PersonValidator.Validate(personDto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        // -----------------------------------------------------
        // 3. Load existing tracked entity
        // -----------------------------------------------------

        var existingPerson =
            await _personRepository
                .GetPersonForUpdateAsync(id);

        if (existingPerson is null)
        {
            return Result.NotFound(
                "Person not found.");
        }

        // -----------------------------------------------------
        // 4. Check NationalNo uniqueness
        // -----------------------------------------------------

        var normalizedNationalNo =
            personDto.NationalNo.Trim();

        var duplicated =
            await _personRepository
                .IsNationalNoDuplicatedAsync(
                    normalizedNationalNo,
                    id);

        if (duplicated)
        {
            return Result.Conflict(
                "The national number is already registered to another person.");
        }

        // -----------------------------------------------------
        // 5. Apply DTO to existing entity
        // -----------------------------------------------------

        PersonMapper.ApplyUpdate(
            personDto,
            existingPerson);

        // -----------------------------------------------------
        // 6. Persist
        // -----------------------------------------------------

        var success =
            await _personRepository
                .UpdatePersonAsync(
                    existingPerson);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to update person.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result> DeletePersonAsync(int id)
    {
        if (id <= 0)
            return Result.ValidationFailure(
                "Invalid person ID.");

        if (!await _personRepository.IsPersonExistsByIdAsync(id))
            return Result.NotFound(
                "Person not found.");

        // Person cannot be deleted if he/she
        // has any application.
        if (await _personRepository.HasApplicationsAsync(id))
        {
            return Result.Conflict(
                "Cannot delete this person because they have one or more applications.");
        }

        var success =
            await _personRepository.DeletePersonAsync(id);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to delete person.");
    }
}