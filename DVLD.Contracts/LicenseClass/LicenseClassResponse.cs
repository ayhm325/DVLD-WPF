namespace DVLD.Contracts.LicenseClass;

public sealed class LicenseClassResponse
{
    public int LicenseClassId { get; init; }

    public string LicenseClassName { get; init; } = string.Empty;

    public string LicenseClassDescription { get; init; } = string.Empty;

    public byte MinAllowedAge { get; init; }

    public byte DefaultValidityLength { get; init; }

    public decimal LicenseClassFees { get; init; }
}