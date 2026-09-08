namespace DVLD.Contracts.Driver;

public sealed class DriverResponse
{
    public int DriverId { get; init; }

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string NationalNo { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string Gender { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public int ActiveLicenses { get; init; }

    public int CreatedByUserId { get; init; }

    public string CreatedByUserName { get; init; } = string.Empty;

    public DateTime CreatedDate { get; init; }
}
