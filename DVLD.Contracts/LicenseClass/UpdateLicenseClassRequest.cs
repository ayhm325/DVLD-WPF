namespace DVLD.Contracts.LicenseClass;

public sealed class UpdateLicenseClassRequest
{
    public int LicenseClassId { get; set; }

    public string LicenseClassName { get; set; } = string.Empty;

    public string LicenseClassDescription { get; set; } = string.Empty;

    public byte MinAllowedAge { get; set; }

    public byte DefaultValidityLength { get; set; }

    public decimal LicenseClassFees { get; set; }
}
