using Application.DTOs.UserDTO;
using Domain.Entities;

namespace Application.Mappings;

public static class UserMapper
{
    public static UserDto ToDto(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var fullName = string.Join(
            " ",
            new[]
            {
                user.Person?.FirstName,
                user.Person?.SecondName,
                user.Person?.ThirdName,
                user.Person?.LastName
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        return new UserDto
        {
            UserId = user.UserId,
            PersonId = user.PersonId,
            UserName = user.UserName,
            IsActive = user.IsActive,
            FullName = fullName,
            Role = user.Role
        };
    }

    public static User ToEntity(
        CreateUserDto dto,
        string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException(
                "Hashed password is required.",
                nameof(hashedPassword));
        }

        return new User
        {
            PersonId = dto.PersonId,
            UserName = dto.UserName.Trim(),
            Password = hashedPassword,
            IsActive = dto.IsActive,
            Role = Domain.Enums.UserRole.Staff
        };
    }

    public static UserProfileDto ToProfileDto(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var person = user.Person;

        return new UserProfileDto
        {
            UserId = user.UserId,
            PersonId = user.PersonId,
            UserName = user.UserName,
            IsActive = user.IsActive,

            FullName = person?.FullName ?? string.Empty,

            NationalNo = person?.NationalNo ?? string.Empty,
            FirstName = person?.FirstName ?? string.Empty,
            SecondName = person?.SecondName ?? string.Empty,
            ThirdName = person?.ThirdName,
            LastName = person?.LastName ?? string.Empty,

            DateOfBirth = person?.DateOfBirth ?? default,
            Gender = person is null ? 0 : (int)person.Gender,

            Address = person?.Address ?? string.Empty,
            Phone = person?.Phone ?? string.Empty,
            Email = person?.Email,

            NationalityCountryID = person?.NationalityCountryID ?? 0,
            CountryName = person?.Country?.CountryName,
            ImagePath = person?.ImagePath
        };
    }
}