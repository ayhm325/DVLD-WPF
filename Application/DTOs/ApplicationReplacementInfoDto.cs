namespace Application.DTOs
{
    public class ApplicationReplacementInfoDto
    {
        public int? ReplacementApplicationID { get; set; }

        public int OldLicenseID { get; set; }

        public int? ReplacementLicenseID { get; set; }

        public DateTime ApplicationDate { get; set; }

        public decimal ApplicationFees { get; set; }

        public decimal LicenseFees { get; set; }

        public string ReplacementReason { get; set; } = string.Empty;

        public string CreatedByUserName { get; set; } = string.Empty;
    }
}