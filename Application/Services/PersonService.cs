using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;

    public PersonService(IPersonRepository personRepository)
    {
        _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
    }

    // Get All
    public async Task<Result<List<PersonDto>>> GetAllPeopleAsync()
    {
        var people = await _personRepository.GetAllPersonsAsync();
        return Result<List<PersonDto>>.Success(people.Select(PersonMapper.ToDto).ToList());
    }

    // Get By Id
    public async Task<Result<PersonDto>> GetPersonByIdAsync(int id)
    {
        if (id <= 0) return Result<PersonDto>.ValidationFailure("Invalid person ID.");

        var person = await _personRepository.GetPersonByIdAsync(id);
        return person is null
            ? Result<PersonDto>.NotFound("Person not found.")
            : Result<PersonDto>.Success(PersonMapper.ToDto(person));
    }

    // Get By National No
    public async Task<Result<PersonDto>> GetPersonByNationalNoAsync(string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
            return Result<PersonDto>.ValidationFailure("National number is required.");

        var person = await _personRepository.GetPersonByNationalNoAsync(nationalNo.Trim());
        return person is null
            ? Result<PersonDto>.NotFound("Person not found.")
            : Result<PersonDto>.Success(PersonMapper.ToDto(person));
    }

    // Check Exists
    public async Task<bool> IsPersonExistsAsync(int id) =>
        id > 0 && await _personRepository.IsPersonExistsByIdAsync(id);

    // Create
    public async Task<Result<int>> AddPersonAsync(PersonCreateUpdateDto dto)
    {
        if (dto is null) return Result<int>.ValidationFailure("Person data is required.");

        var validation = PersonValidator.Validate(dto);
        if (validation.IsFailure) return Result<int>.ValidationFailure(validation.Error);

        var person = PersonMapper.ToEntity(dto);

        // التحقق من عدم تكرار الرقم الوطني
        if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, 0))
            return Result<int>.Conflict("The national number is already registered.");

        var personId = await _personRepository.AddPersonAsync(person);
        return Result<int>.Success(personId);
    }

    // Update
    public async Task<Result> UpdatePersonAsync(int id, PersonCreateUpdateDto dto)
    {
        if (id <= 0) return Result.ValidationFailure("Invalid person ID.");
        if (dto is null) return Result.ValidationFailure("Person data is required.");
        if (!await _personRepository.IsPersonExistsByIdAsync(id))
            return Result.NotFound("Person not found.");

        var validation = PersonValidator.Validate(dto);
        if (validation.IsFailure) return Result.ValidationFailure(validation.Error);

        var person = PersonMapper.ToEntity(dto);
        person.PersonId = id; // التأكد من استخدام الـ ID من الرابط

        // التحقق من عدم تكرار الرقم الوطني مع استبعاد الشخص الحالي
        if (await _personRepository.IsNationalNoDuplicatedAsync(person.NationalNo, id))
            return Result.Conflict("The national number is already registered to another person.");

        var success = await _personRepository.UpdatePersonAsync(person);
        return success ? Result.Success() : Result.Failure("Failed to update person.");
    }

    // Delete
    public async Task<Result> DeletePersonAsync(int id)
    {
        if (id <= 0) return Result.ValidationFailure("Invalid person ID.");
        if (!await _personRepository.IsPersonExistsByIdAsync(id))
            return Result.NotFound("Person not found.");

        var success = await _personRepository.DeletePersonAsync(id);
        return success ? Result.Success() : Result.Failure("Failed to delete person.");
    }
}