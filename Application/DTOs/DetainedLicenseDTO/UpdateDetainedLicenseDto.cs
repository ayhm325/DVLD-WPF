namespace Application.DTOs.DetainedLicenseDTO;

public class UpdateDetainedLicenseDto
{
    public int DetainID { get; set; }

    public decimal FineFees { get; set; }

    public bool IsReleased { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? ReleasedByUserID { get; set; }

    public int? ReleaseApplicationID { get; set; }
}