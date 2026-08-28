using Application.DTOs.UserDTO;
using Domain.Entities;

namespace Application.Mappings;

public static class UserMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static UserDto ToDto(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            UserId =
                user.UserId,

            PersonId =
                user.PersonId,

            UserName =
                user.UserName,

            IsActive =
                user.IsActive,

            FullName =
                user.Person is null
                    ? string.Empty
                    : string.Join(
                        " ",
                        new[]
                        {
                            user.Person.FirstName,
                            user.Person.SecondName,
                            user.Person.ThirdName,
                            user.Person.LastName
                        }
                        .Where(
                            x => !string.IsNullOrWhiteSpace(x)))
        };
    }


    // =========================================================
    // CREATE DTO -> ENTITY
    // =========================================================

    public static User ToEntity(
        CreateUserDto dto,
        string hashedPassword)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException(
                "Hashed password is required.",
                nameof(hashedPassword));

        return new User
        {
            PersonId =
                dto.PersonId,

            UserName =
                dto.UserName.Trim(),

            Password =
                hashedPassword,

            IsActive =
                dto.IsActive
        };
    }
}

