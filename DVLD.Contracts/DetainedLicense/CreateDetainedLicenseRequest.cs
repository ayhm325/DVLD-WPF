namespace DVLD.Contracts.DetainedLicense;

public sealed class CreateDetainedLicenseRequest
{
    public int LicenseId { get; init; }

    public decimal FineFees { get; init; }
}