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
}