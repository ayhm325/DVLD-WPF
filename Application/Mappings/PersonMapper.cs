using Application.DTOs.PersonDTO;
using Domain.Entities;

namespace Application.Mappings;

public static class PersonMapper
{
    // Entity -> DTO
    public static PersonDto ToDto(Person person)
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

    // DTO -> Entity
    public static Person ToEntity(PersonCreateUpdateDto dto)
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