namespace DVLD.Contracts.LocalDrivingLicenseApplication;

public sealed class LocalDrivingLicenseApplicationResponse
{
    public int LocalDrivingLicenseApplicationId { get; init; }

    public int LicenseClassId { get; init; }

    public string? LicenseClassName { get; init; }

    public string? NationalNo { get; init; }

    public string? FullName { get; init; }

    public DateTime ApplicationDate { get; init; }

    public int PassedTest { get; init; }

    public string ApplicationStatus { get; init; } = string.Empty;

    public string StatusText { get; init; } = string.Empty;

    public decimal ApplicationFees { get; init; }

    public decimal LicenseClassFees { get; init; }

    public bool HasLicense { get; init; }

    public int ApplicantPersonId { get; init; }
}