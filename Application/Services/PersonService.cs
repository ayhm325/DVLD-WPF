using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<PersonService> _logger;
    private readonly IJordanClock _clock;

    public PersonService(
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IImageProcessingService imageProcessingService,
        ILogger<PersonService> logger,
        IJordanClock clock)
    {
        _personRepository =
            personRepository
            ?? throw new ArgumentNullException(
                nameof(personRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        _fileStorageService =
            fileStorageService
            ?? throw new ArgumentNullException(
                nameof(fileStorageService));

        _imageProcessingService =
            imageProcessingService
            ?? throw new ArgumentNullException(
                nameof(imageProcessingService));

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        _clock =
            clock
            ?? throw new ArgumentNullException(
                nameof(clock));
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
                personDto,
                _clock.Today);

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
                personDto,
                _clock.Today);

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

        bool hasChanges =
            !string.Equals(existingPerson.NationalNo, normalizedNationalNo, StringComparison.Ordinal) ||
            !string.Equals(existingPerson.FirstName, personDto.FirstName.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existingPerson.SecondName, personDto.SecondName.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existingPerson.ThirdName ?? string.Empty, (personDto.ThirdName ?? string.Empty).Trim(), StringComparison.Ordinal) ||
            !string.Equals(existingPerson.LastName, personDto.LastName.Trim(), StringComparison.Ordinal) ||
            existingPerson.DateOfBirth.Date != personDto.DateOfBirth.Date ||
            (int)existingPerson.Gender != personDto.Gender ||
            !string.Equals(existingPerson.Address, personDto.Address.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existingPerson.Phone, personDto.Phone.Trim(), StringComparison.Ordinal) ||
            !string.Equals(existingPerson.Email ?? string.Empty, (personDto.Email ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) ||
            existingPerson.NationalityCountryID != personDto.NationalityCountryID;

        if (!hasChanges)
        {
            return Result.Success();
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

        var imagePathToDelete = person.ImagePath;

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

        if (!string.IsNullOrWhiteSpace(imagePathToDelete))
        {
            try
            {
                await _fileStorageService.DeleteAsync(imagePathToDelete);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up image {ImagePath} for deleted person {PersonId}", imagePathToDelete, id);
            }
        }

        return Result.Success();
    }

    public async Task<Result<string>> UploadPersonPhotoAsync(
        int personId,
        Stream photoStream,
        long length,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
        {
            return Result<string>.FromValidationFailure("Invalid person ID.");
        }

        var person = await _personRepository.GetPersonForUpdateAsync(personId);
        if (person is null)
        {
            return Result<string>.FromNotFound("Person not found.");
        }

        var processingResult = _imageProcessingService.ProcessPersonPhoto(
            photoStream,
            length,
            contentType);

        if (processingResult.IsFailure)
        {
            return Result<string>.FromValidationFailure(processingResult.Error);
        }

        var processed = processingResult.Value!;
        string newStorageKey;
        try
        {
            newStorageKey = await _fileStorageService.SaveAsync(
                processed.ImageStream,
                processed.Extension,
                "people",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist processed image file for person {PersonId}", personId);
            return Result<string>.FromFailure("Failed to store image file.");
        }

        var oldImagePath = person.ImagePath;
        person.ImagePath = newStorageKey;

        var saved = await _unitOfWork.SaveChangesAsync();
        if (saved <= 0)
        {
            _logger.LogWarning("Database save failed for person {PersonId} photo update. Performing cleanup of {StorageKey}", personId, newStorageKey);
            await _fileStorageService.DeleteAsync(newStorageKey, cancellationToken);
            return Result<string>.FromFailure("Failed to save person photo reference to the database.");
        }

        if (!string.IsNullOrWhiteSpace(oldImagePath) &&
            !string.Equals(oldImagePath, newStorageKey, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await _fileStorageService.DeleteAsync(oldImagePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up superseded photo {OldKey} for person {PersonId}", oldImagePath, personId);
            }
        }

        return Result<string>.Success(newStorageKey);
    }

    public async Task<Result<(Stream Stream, string ContentType, string StorageKey)>> GetPersonPhotoAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
        {
            return Result<(Stream, string, string)>.FromValidationFailure("Invalid person ID.");
        }

        var person = await _personRepository.GetPersonByIdAsync(personId);
        if (person is null)
        {
            return Result<(Stream, string, string)>.FromNotFound("Person not found.");
        }

        if (string.IsNullOrWhiteSpace(person.ImagePath))
        {
            return Result<(Stream, string, string)>.FromNotFound("Person does not have a photograph.");
        }

        var stream = await _fileStorageService.GetAsync(person.ImagePath, cancellationToken);
        if (stream is null)
        {
            return Result<(Stream, string, string)>.FromNotFound("Photograph file could not be found.");
        }

        string contentType = person.ImagePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? "image/webp"
            : person.ImagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";

        return Result<(Stream, string, string)>.Success((stream, contentType, person.ImagePath));
    }

    public async Task<Result> DeletePersonPhotoAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
        {
            return Result.ValidationFailure("Invalid person ID.");
        }

        var person = await _personRepository.GetPersonForUpdateAsync(personId);
        if (person is null)
        {
            return Result.NotFound("Person not found.");
        }

        if (string.IsNullOrWhiteSpace(person.ImagePath))
        {
            return Result.Success();
        }

        var oldImagePath = person.ImagePath;
        person.ImagePath = null;

        var saved = await _unitOfWork.SaveChangesAsync();
        if (saved <= 0)
        {
            return Result.Failure("Failed to remove photo reference from the database.");
        }

        try
        {
            await _fileStorageService.DeleteAsync(oldImagePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up removed image {OldKey} for person {PersonId}", oldImagePath, personId);
        }

        return Result.Success();
    }
}
