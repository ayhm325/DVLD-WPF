namespace Application.DTOs.InternationalLicenseDTO;

public class UpdateInternationalLicenseDto
{
    public int InternationalLicenseID { get; set; }

    public int ApplicationID { get; set; }

    public int DriverID { get; set; }

    public int IssuedUsingLocalLicenseID { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public bool IsActive { get; set; }
}