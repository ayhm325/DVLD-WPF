using Application.DTOs.PersonDTO;
using Domain.Entities;

namespace Application.Mappings;

public static class PersonMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static PersonDto ToDto(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new PersonDto
        {
            PersonId = person.PersonId,

            NationalNo = person.NationalNo,

            FirstName = person.FirstName,

            SecondName = person.SecondName,

            ThirdName = person.ThirdName,

            LastName = person.LastName,

            FullName = person.FullName,

            DateOfBirth = person.DateOfBirth,

            Gender = person.Gender,

            Address = person.Address,

            Phone = person.Phone,

            Email = person.Email,

            NationalityCountryID =
                person.NationalityCountryID,

            CountryName =
                person.Country?.CountryName,

            ImagePath =
                person.ImagePath
        };
    }

    // =========================================================
    // CREATE DTO -> ENTITY
    // =========================================================

    public static Person ToEntity(
        PersonCreateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Person
        {
            NationalNo =
                NormalizeRequired(dto.NationalNo),

            FirstName =
                NormalizeRequired(dto.FirstName),

            SecondName =
                NormalizeRequired(dto.SecondName),

            ThirdName =
                NormalizeOptional(dto.ThirdName),

            LastName =
                NormalizeRequired(dto.LastName),

            DateOfBirth =
                dto.DateOfBirth,

            Gender =
                dto.Gender,

            Address =
                NormalizeRequired(dto.Address),

            Phone =
                NormalizeRequired(dto.Phone),

            Email =
                NormalizeOptional(dto.Email),

            NationalityCountryID =
                dto.NationalityCountryID,

            ImagePath =
                NormalizeOptional(dto.ImagePath)
        };
    }

    // =========================================================
    // UPDATE DTO -> EXISTING ENTITY
    // =========================================================

    public static void ApplyUpdate(
        PersonUpdateDto dto,
        Person person)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(person);

        person.NationalNo =
            NormalizeRequired(dto.NationalNo);

        person.FirstName =
            NormalizeRequired(dto.FirstName);

        person.SecondName =
            NormalizeRequired(dto.SecondName);

        person.ThirdName =
            NormalizeOptional(dto.ThirdName);

        person.LastName =
            NormalizeRequired(dto.LastName);

        person.DateOfBirth =
            dto.DateOfBirth;

        person.Gender =
            dto.Gender;

        person.Address =
            NormalizeRequired(dto.Address);

        person.Phone =
            NormalizeRequired(dto.Phone);

        person.Email =
            NormalizeOptional(dto.Email);

        person.NationalityCountryID =
            dto.NationalityCountryID;

        person.ImagePath =
            NormalizeOptional(dto.ImagePath);
    }

    // =========================================================
    // NORMALIZATION
    // =========================================================

    private static string NormalizeRequired(
        string? value)
    {
        return value?.Trim()
            ?? string.Empty;
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}