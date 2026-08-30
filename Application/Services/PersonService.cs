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
        GetPersonByIdAsync(
            int id)
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
        IsPersonExistsAsync(
            int id)
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
        // -----------------------------------------------------
        // VALIDATE INPUT
        // -----------------------------------------------------

        if (personDto is null)
        {
            return Result<int>
                .FromValidationFailure(
                    "Person data is required.");
        }

        // -----------------------------------------------------
        // VALIDATE DTO
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
        // MAP DTO -> ENTITY
        // -----------------------------------------------------

        var person =
            PersonMapper.ToEntity(
                personDto);

        // -----------------------------------------------------
        // CHECK NATIONAL NUMBER
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
        // ADD TO CURRENT DbContext
        // -----------------------------------------------------

        await _personRepository
            .AddPersonAsync(person);

        // -----------------------------------------------------
        // PERSIST THROUGH UOW
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
        // VALIDATE ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return Result
                .ValidationFailure(
                    "Invalid person ID.");
        }

        // -----------------------------------------------------
        // VALIDATE DTO
        // -----------------------------------------------------

        if (personDto is null)
        {
            return Result
                .ValidationFailure(
                    "Person data is required.");
        }

        var validation =
            PersonValidator.Validate(
                personDto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // LOAD TRACKED ENTITY
        // -----------------------------------------------------

        var existingPerson =
            await _personRepository
                .GetPersonForUpdateAsync(id);

        if (existingPerson is null)
        {
            return Result
                .NotFound(
                    "Person not found.");
        }

        // -----------------------------------------------------
        // CHECK NATIONAL NUMBER UNIQUENESS
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
            return Result
                .Conflict(
                    "The national number is already registered to another person.");
        }

        // -----------------------------------------------------
        // APPLY CHANGES TO TRACKED ENTITY
        // -----------------------------------------------------

        PersonMapper.ApplyUpdate(
            personDto,
            existingPerson);

        // -----------------------------------------------------
        // PERSIST THROUGH UOW
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result
                .Failure(
                    "No changes were saved.");
        }

        return Result.Success();
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeletePersonAsync(
            int id)
    {
        // -----------------------------------------------------
        // VALIDATE ID
        // -----------------------------------------------------

        if (id <= 0)
        {
            return Result
                .ValidationFailure(
                    "Invalid person ID.");
        }

        // -----------------------------------------------------
        // CHECK PERSON EXISTS
        // -----------------------------------------------------

        if (!await _personRepository
                .IsPersonExistsByIdAsync(id))
        {
            return Result
                .NotFound(
                    "Person not found.");
        }

        // -----------------------------------------------------
        // BUSINESS RULE
        //
        // Person cannot be deleted when applications exist.
        // -----------------------------------------------------

        if (await _personRepository
                .HasApplicationsAsync(id))
        {
            return Result
                .Conflict(
                    "Cannot delete this person because they have one or more applications.");
        }

        // -----------------------------------------------------
        // DELETE FROM DbContext
        // -----------------------------------------------------

        var removed =
            await _personRepository
                .DeletePersonAsync(id);

        if (!removed)
        {
            return Result
                .Failure(
                    "Failed to delete person.");
        }

        // -----------------------------------------------------
        // PERSIST THROUGH UOW
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result
                .Failure(
                    "Failed to save person deletion.");
        }

        return Result.Success();
    }
}