using Domain.Enums;

namespace Application.DTOs.InternationalLicenseDTO;

public class InternationalLicenseApplicationInfoDto
{
    public int ApplicationID { get; set; }

    public int InternationalLicenseID { get; set; }

    public int LocalLicenseID { get; set; }

    public DateTime ApplicationDate { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public AppStatus ApplicationStatus { get; set; }

    public DateTime LastStatusDate { get; set; }

    public decimal PaidFees { get; set; }

    public int CreatedByUserID { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;

    public string StatusText => ApplicationStatus switch
    {
        AppStatus.New => "New",
        AppStatus.Cancelled => "Cancelled",
        AppStatus.Completed => "Completed",
        _ => "Unknown"
    };
}