namespace DVLD.Contracts.Person;

public sealed class PersonListResponse
{
    public int PersonId { get; init; }
    public string NationalNo { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? CountryName { get; init; }
}