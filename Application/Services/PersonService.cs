using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services
{
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
                await _personRepository.GetAllPersonsAsync();

            var personDtos =
                people
                    .Select(MapToDto)
                    .ToList();

            return Result<List<PersonDto>>.Success(
                personDtos);
        }


        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<Result<PersonDto>>
            GetPersonByIdAsync(int id)
        {
            if (id <= 0)
            {
                return Result<PersonDto>.Fail(
                    "Invalid person ID.");
            }

            var person =
                await _personRepository
                    .GetPersonByIdAsync(id);

            if (person is null)
            {
                return Result<PersonDto>.Fail(
                    "Person not found.");
            }

            return Result<PersonDto>.Success(
                MapToDto(person));
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
                return Result<PersonDto>.Fail(
                    "National number is required.");
            }

            nationalNo = nationalNo.Trim();

            var person =
                await _personRepository
                    .GetPersonByNationalNoAsync(
                        nationalNo);

            if (person is null)
            {
                return Result<PersonDto>.Fail(
                    "Person not found.");
            }

            return Result<PersonDto>.Success(
                MapToDto(person));
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
        // ADD
        // =========================================================

        public async Task<Result<int>>
            AddPersonAsync(
                PersonCreateUpdateDto dto)
        {
            if (dto is null)
            {
                return Result<int>.Fail(
                    "Person data is required.");
            }

            var person =
                MapToEntity(dto);

            // Validate
            var validation =
                PersonValidator.Validate(person);

            if (!validation.IsValid)
            {
                return Result<int>.Fail(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            // National number uniqueness
            var nationalNoExists =
                await _personRepository
                    .IsNationalNoDuplicatedAsync(
                        person.NationalNo,
                        0);

            if (nationalNoExists)
            {
                return Result<int>.Fail(
                    "The national number is already registered.");
            }

            // Save
            var personId =
                await _personRepository
                    .AddPersonAsync(person);

            return Result<int>.Success(
                personId);
        }


        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<Result>
            UpdatePersonAsync(
                int id,
                PersonCreateUpdateDto dto)
        {
            if (id <= 0)
            {
                return Result.Failure(
                    "Invalid person ID.");
            }

            if (dto is null)
            {
                return Result.Failure(
                    "Person data is required.");
            }

            // Check existence
            var exists =
                await _personRepository
                    .IsPersonExistsByIdAsync(id);

            if (!exists)
            {
                return Result.Failure(
                    "Person not found.");
            }

            // Map
            var person =
                MapToEntity(dto);

            person.PersonId = id;

            // Validate
            var validation =
                PersonValidator.Validate(person);

            if (!validation.IsValid)
            {
                return Result.Failure(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            // National number uniqueness
            var nationalNoExists =
                await _personRepository
                    .IsNationalNoDuplicatedAsync(
                        person.NationalNo,
                        id);

            if (nationalNoExists)
            {
                return Result.Failure(
                    "The national number is already registered to another person.");
            }

            // Update
            var success =
                await _personRepository
                    .UpdatePersonAsync(person);

            return success
                ? Result.Success()
                : Result.Failure(
                    "Failed to update person.");
        }


        // =========================================================
        // DELETE
        // =========================================================

        public async Task<Result>
            DeletePersonAsync(int id)
        {
            if (id <= 0)
            {
                return Result.Failure(
                    "Invalid person ID.");
            }

            var exists =
                await _personRepository
                    .IsPersonExistsByIdAsync(id);

            if (!exists)
            {
                return Result.Failure(
                    "Person not found.");
            }

            var success =
                await _personRepository
                    .DeletePersonAsync(id);

            return success
                ? Result.Success()
                : Result.Failure(
                    "Failed to delete person.");
        }


        // =========================================================
        // ENTITY -> DTO
        // =========================================================

        private static PersonDto MapToDto(
            Person person)
        {
            return new PersonDto
            {
                PersonId =
                    person.PersonId,

                NationalNo =
                    person.NationalNo,

                FullName =
                    person.FullName,

                DateOfBirth =
                    person.DateOfBirth,

                Gender =
                    person.Gender,

                Address =
                    person.Address,

                Phone =
                    person.Phone,

                Email =
                    person.Email,

                CountryName =
                    person.Country?.CountryName
                    ?? "Unknown",

                NationalityCountryID =
                    person.NationalityCountryID,

                ImagePath =
                    person.ImagePath
            };
        }


        // =========================================================
        // DTO -> ENTITY
        // =========================================================

        private static Person MapToEntity(
            PersonCreateUpdateDto dto)
        {
            return new Person
            {
                PersonId =
                    dto.PersonId,

                NationalNo =
                    dto.NationalNo?.Trim()
                    ?? string.Empty,

                FirstName =
                    dto.FirstName?.Trim()
                    ?? string.Empty,

                SecondName =
                    dto.SecondName?.Trim()
                    ?? string.Empty,

                ThirdName =
                    string.IsNullOrWhiteSpace(dto.ThirdName)
                        ? null
                        : dto.ThirdName.Trim(),

                LastName =
                    dto.LastName?.Trim()
                    ?? string.Empty,

                DateOfBirth =
                    dto.DateOfBirth,

                Gender =
                    dto.Gender,

                Address =
                    dto.Address?.Trim()
                    ?? string.Empty,

                Phone =
                    dto.Phone?.Trim()
                    ?? string.Empty,

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
}