using Application.DTOs.UserDTO;
using Domain.Entities;

namespace Application.Mappings;

public static class UserMapper
{
    // Entity -> DTO
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            PersonId = user.PersonId,
            UserName = user.UserName,
            IsActive = user.IsActive,

            // دمج أجزاء الاسم مع تجاهل الأجزاء الفارغة
            FullName = user.Person is null
                ? string.Empty
                : string.Join(" ", new[]
                {
                    user.Person.FirstName,
                    user.Person.SecondName,
                    user.Person.ThirdName,
                    user.Person.LastName
                }.Where(x => !string.IsNullOrWhiteSpace(x)))
        };
    }

    // Create DTO -> Entity
    public static User ToEntity(CreateUserDto dto, string hashedPassword)
    {
        return new User
        {
            PersonId = dto.PersonId,
            UserName = dto.UserName.Trim(),
            Password = hashedPassword,
            IsActive = dto.IsActive
        };
    }
}