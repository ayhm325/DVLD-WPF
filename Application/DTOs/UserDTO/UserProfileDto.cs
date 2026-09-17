namespace Application.DTOs.UserDTO;

public sealed class UserProfileDto
{
    public int UserId { get; init; }
    public int PersonId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string FullName { get; init; } = string.Empty;

    public string NationalNo { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string SecondName { get; init; } = string.Empty;
    public string? ThirdName { get; init; }
    public string LastName { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }
    public int Gender { get; init; }

    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }

    public int NationalityCountryID { get; init; }
    public string? CountryName { get; init; }
    public string? ImagePath { get; init; }
}