namespace DVLD.Contracts.InternationalLicense;

public sealed class InternationalLicenseResponse
{
    public int InternationalLicenseId { get; init; }

    public int ApplicationId { get; init; }

    public int DriverId { get; init; }

    public int IssuedUsingLocalLicenseId { get; init; }

    public DateTime IssueDate { get; init; }

    public DateTime ExpirationDate { get; init; }

    public bool IsActive { get; init; }

    public int CreatedByUserId { get; init; }

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public DateTime DateOfBirth { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public string NationalNo { get; init; } = string.Empty;

    public string Gender { get; init; } = string.Empty;

    public decimal Fees { get; init; }

    public string CreatedByUserName { get; init; } = string.Empty;
}
