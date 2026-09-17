namespace DVLD.Contracts.User;

public sealed record UserProfileResponse(
    int UserId,
    int PersonId,
    string UserName,
    bool IsActive,
    string FullName,
    string NationalNo,
    string FirstName,
    string SecondName,
    string? ThirdName,
    string LastName,
    DateTime DateOfBirth,
    int Gender,
    string Address,
    string Phone,
    string? Email,
    int NationalityCountryID,
    string? CountryName,
    string? ImagePath);