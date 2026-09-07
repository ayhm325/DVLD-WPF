namespace DVLD.Contracts.Person;

public sealed class CreatePersonRequest
{
    public string NationalNo { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string SecondName { get; init; } = string.Empty;

    public string? ThirdName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }

    public Gender Gender { get; init; }

    public string Address { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string? Email { get; init; }

    public int NationalityCountryID { get; init; }

    public string? ImagePath { get; init; }
}
