namespace Application.DTOs;

public class LicenseClassDto
{
    public int LicenseClassID { get; set; }

    public string LicenseClassName { get; set; } = string.Empty;

    public string LicenseClassDescription { get; set; } = string.Empty;

    public byte MinAllowedAge { get; set; }

    public byte DefaultValidityLength { get; set; }

    public decimal LicenseClassFees { get; set; }
}