


namespace Application.DTOs
{
    public class DriverLicenseInfoDto
    {
        // License Info
        public int LicenseId { get; set; }
        public string LicenseClass { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDetained { get; set; }
        public string IssueReason { get; set; } = string.Empty;
        public string? Notes { get; set; } 

        // Driver Info
        public int DriverId { get; set; }

        // Person Info
        public int PersonID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalNo { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? ImagePath { get; set; }

    }
}
