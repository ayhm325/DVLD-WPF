namespace Domain.Entities
{
    public class License
    {
        public int LicenseID { get; set; }


        public int ApplicationID { get; set; }


        public int DriverID { get; set; }


        // FK to LicenseClass
        public int LicenseClass { get; set; }


        public DateTime IssueDate { get; set; }


        public DateTime ExpirationDate { get; set; }


        public string? Notes { get; set; }


        public decimal PaidFees { get; set; }


        public bool IsActive { get; set; }


        public byte IssueReason { get; set; }


        public int CreatedByUserID { get; set; }



        // Navigation Properties

        public virtual ApplicationD Application { get; set; } = null!;


        public virtual Driver Driver { get; set; } = null!;


        public virtual LicenseClass LicenseClassInfo { get; set; } = null!;


        public virtual User CreatedByUser { get; set; } = null!;
    }
}