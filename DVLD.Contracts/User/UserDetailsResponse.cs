namespace DVLD.Contracts.Users;

public sealed record UserDetailsResponse(
    int UserId,
    int PersonId,
    string UserName,
    bool IsActive,
    int Role,
    string NationalNo,
    string FirstName,
    string SecondName,
    string? ThirdName,
    string LastName,
    DateTime DateOfBirth,
    string Gender,
    string Address,
    string Phone,
    string? Email,
    int NationalityCountryID,
    string? CountryName,
    string? ImagePath);