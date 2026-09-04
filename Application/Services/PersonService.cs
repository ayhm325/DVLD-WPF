using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public sealed class PersonService : IPersonService
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

    public async Task<Result<List<PersonDto>>>
        GetAllPeopleAsync()
    {
        var people =
            await _personRepository
                .GetAllPersonsAsync();

        var peopleDto =
            people
                .Select(PersonMapper.ToDto)
                .ToList();

        return Result<List<PersonDto>>
            .Success(peopleDto);
    }

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

    public Task<bool>
        IsPersonExistsAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult(false);

        return _personRepository
            .IsPersonExistsByIdAsync(id);
    }

    public async Task<Result<int>>
        AddPersonAsync(
            PersonCreateDto personDto)
    {
        var validation =
            PersonValidator.Validate(
                personDto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        var person =
            PersonMapper.ToEntity(
                personDto!);

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

        await _personRepository
            .AddPersonAsync(person);

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

    public async Task<Result>
        UpdatePersonAsync(
            int id,
            PersonUpdateDto personDto)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid person ID.");
        }

        var validation =
            PersonValidator.Validate(
                personDto);

        if (validation.IsFailure)
            return validation;

        var existingPerson =
            await _personRepository
                .GetPersonForUpdateAsync(id);

        if (existingPerson is null)
        {
            return Result.NotFound(
                "Person not found.");
        }

        var normalizedNationalNo =
            personDto!.NationalNo.Trim();

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

        PersonMapper.ApplyUpdate(
            personDto,
            existingPerson);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure(
                "No changes were saved.");
        }

        return Result.Success();
    }

    public async Task<Result>
        DeletePersonAsync(int id)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid person ID.");
        }

        var person =
            await _personRepository
                .GetPersonForUpdateAsync(id);

        if (person is null)
        {
            return Result.NotFound(
                "Person not found.");
        }

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

        if (saved <= 0)
        {
            return Result.Failure(
                "Failed to save person deletion.");
        }

        return Result.Success();
    }
}