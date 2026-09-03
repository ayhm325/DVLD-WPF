namespace Application.DTOs.LicenseDTO;

public class CreateLicenseDto
{
    public int ApplicationID { get; set; }
    public int DriverID { get; set; }
    public int LicenseClassID { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? Notes { get; set; }
    public decimal PaidFees { get; set; }
    public bool IsActive { get; set; }
    public byte IssueReason { get; set; }
}