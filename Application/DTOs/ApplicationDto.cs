using Domain.Enums;

public class ApplicationDto
{
    public int ApplicationID { get; set; }

    public int ApplicantPersonID { get; set; }

    public DateTime ApplicationDate { get; set; }

    public int ApplicationTypeID { get; set; }

    public AppStatus ApplicationStatus { get; set; }

    public DateTime LastStatusDate { get; set; }

    public decimal PaidFees { get; set; }

    public int CreatedByUserID { get; set; }


    // إضافات للعرض
    public int? LicenseID { get; set; }

    public int? LocalLicenseID { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;


    public string StatusText => ApplicationStatus switch
    {
        AppStatus.New => "New",
        AppStatus.Cancelled => "Cancelled",
        AppStatus.Completed => "Completed",
        _ => "Unknown"
    };
}