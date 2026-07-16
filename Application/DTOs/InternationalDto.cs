
namespace Application.DTOs
{
    public class InternationalDto
    {
        public int InternationalLicenseID { get; set; }

        public int ApplicationID { get; set; }

        public int DriverID { get; set; }

        public int IssuedUsingLocalLicenseID { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        public bool IsActive { get; set; }

        public int CreatedByUserID { get; set; }

        public int PersonID { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string ImagePath { get; set; } = string.Empty;

        public string NationalNo { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public decimal Fees { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;
    }
}