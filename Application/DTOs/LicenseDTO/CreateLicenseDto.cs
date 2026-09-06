using Domain.Enums;

namespace Application.DTOs.LicenseDTO;

public sealed class CreateLicenseDto
{
    public int ApplicationID { get; set; }

    public int DriverID { get; set; }

    public int LicenseClassID { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public string? Notes { get; set; }

    public decimal PaidFees { get; set; }

    public IssueReason IssueReason { get; set; }
}