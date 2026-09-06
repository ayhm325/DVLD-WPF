using Application.DTOs.DriverDTO;

namespace Application.DTOs.LicenseDTO;

public sealed class LicenseDto
{
    public int LicenseID { get; set; }

    public int ApplicationID { get; set; }

    public int DriverID { get; set; }

    public string? DriverName { get; set; }

    public int LicenseClassID { get; set; }

    public string? LicenseClassName { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public string? Notes { get; set; }

    public decimal PaidFees { get; set; }

    public bool IsActive { get; set; }

    public byte IssueReason { get; set; }

    public string? IssueReasonText { get; set; }

    public int CreatedByUserID { get; set; }

    public string? CreatedByUserName { get; set; }

    public DriverDto? Driver { get; set; }
}