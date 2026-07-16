

namespace Application.DTOs
{
    public class ApplicationNewLicenseInfoDto
    {
        // Application Info
        public int RenewedLicenseApplicationID { get; set; }

        public int RenewedLicenseID { get; set; }

        public DateTime ApplicationDate { get; set; }


        // Old License Info
        public int OldLicenseID { get; set; }


        // License Dates
        public DateTime IssueDate { get; set; }

        public DateTime ExpirationDate { get; set; }


        // Fees
        public decimal ApplicationFees { get; set; }

        public decimal LicenseFees { get; set; }

        public decimal TotalFees
            => ApplicationFees + LicenseFees;


        // User
        public string CreatedByUserName { get; set; } = string.Empty;


        // Notes
        public string? Notes { get; set; }
    }
}