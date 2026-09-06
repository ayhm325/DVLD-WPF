using Domain.Enums;

namespace Application.DTOs.UserDTO;

public sealed class UserDto
{
    public int UserId { get; init; }

    public int PersonId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public string FullName { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}