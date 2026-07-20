namespace Application.DTOs
{
    public class LicenseDto
    {
        public int LicenseID { get; set; }

        public int ApplicationID { get; set; }
        public string? ApplicationInfo { get; set; }   // نص جاهز للعرض (Person + Type)

        public int DriverID { get; set; }
        public string? DriverName { get; set; }

        public int LicenseClassID { get; set; }
        public string? LicenseClassName { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }

        public string? Notes { get; set; }

        public decimal PaidFees { get; set; }

        public bool IsActive { get; set; }
        public string Status => IsActive ? "Active" : "Inactive";

        public byte IssueReason { get; set; }
        public string? IssueReasonText { get; set; }

        public int CreatedByUserID { get; set; }
        public string? CreatedByUserName { get; set; }

        public DriverDto? Driver { get; set; }

        // =========================
        // UI HELPERS
        // =========================
        public bool IsExpired => DateTime.Now > ExpirationDate;

        public string ExpiryStatus =>
            IsExpired ? "Expired" : "Valid";

        public int RemainingDays =>
            (ExpirationDate - DateTime.Now).Days;
    }
}