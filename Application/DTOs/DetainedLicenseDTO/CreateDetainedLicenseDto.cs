namespace Application.DTOs.DetainedLicenseDTO;

public sealed class CreateDetainedLicenseDto
{
    public int LicenseID { get; set; }
    public decimal FineFees { get; set; }
}