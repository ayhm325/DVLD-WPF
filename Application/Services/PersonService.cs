using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PersonService(
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork)
    {
        _personRepository =
            personRepository
            ?? throw new ArgumentNullException(
                nameof(personRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
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
        // 1. Validate
        // -----------------------------------------------------

        var validation =
            PersonValidator.Validate(
                personDto);

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
            PersonMapper.ToEntity(
                personDto);

        // -----------------------------------------------------
        // 3. Check National Number
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
        // 4. Add to DbContext
        // -----------------------------------------------------

        await _personRepository
            .AddPersonAsync(person);

        // -----------------------------------------------------
        // 5. Persist through UnitOfWork
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0 ||
            person.PersonId <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create person.");
        }

        return Result<int>
            .Success(
                person.PersonId);
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
            PersonValidator.Validate(
                personDto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        // -----------------------------------------------------
        // 3. Load tracked entity
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
        // 4. Check National Number uniqueness
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
        // 5. Apply update to tracked entity
        // -----------------------------------------------------

        PersonMapper.ApplyUpdate(
            personDto,
            existingPerson);

        // -----------------------------------------------------
        // 6. Persist through UnitOfWork
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "No changes were saved.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeletePersonAsync(int id)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid person ID.");
        }

        if (!await _personRepository
                .IsPersonExistsByIdAsync(id))
        {
            return Result.NotFound(
                "Person not found.");
        }

        // Person cannot be deleted if they have
        // one or more applications.

        if (await _personRepository
                .HasApplicationsAsync(id))
        {
            return Result.Conflict(
                "Cannot delete this person because they have one or more applications.");
        }

        var removed =
            await _personRepository
                .DeletePersonAsync(id);

        if (!removed)
        {
            return Result.Failure(
                "Failed to delete person.");
        }

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to save person deletion.");
    }
}