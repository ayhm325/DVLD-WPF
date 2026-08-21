namespace Application.DTOs.DetainedLicenseDTO;

public class CreateDetainedLicenseDto
{
    public int LicenseID { get; set; }

    public DateTime DetainDate { get; set; }

    public decimal FineFees { get; set; }

    public int CreatedByUserID { get; set; }
}