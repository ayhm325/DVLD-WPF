namespace DVLD.Contracts.License;

public sealed class DriverLicenseInfoResponse
{
    public int LicenseId { get; init; }

    public string LicenseClass { get; init; } = string.Empty;

    public DateTime IssueDate { get; init; }

    public DateTime ExpirationDate { get; init; }

    public bool IsActive { get; init; }

    public bool IsDetained { get; init; }

    public string IssueReason { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public decimal LicenseClassFees { get; init; }

    public int DriverId { get; init; }

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string NationalNo { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string Gender { get; init; } = string.Empty;

    public string? ImagePath { get; init; }
}